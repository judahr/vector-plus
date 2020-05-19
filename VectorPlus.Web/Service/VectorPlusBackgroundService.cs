using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Anki.Vector;
using Anki.Vector.Exceptions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VectorPlus.Lib;
using VectorPlus.Web.Service.Db;
using VectorPlus.Web.Service.Helpers;

namespace VectorPlus.Web.Service
{
    public class VectorPlusBackgroundService : BackgroundService, IVectorPlusBackgroundService
    {
        private ILogger<VectorPlusBackgroundService> logger;
        private CancellationToken executionStoppingToken;

        private VectorControllerPlusConfig controllerConfig = new VectorControllerPlusConfig() { ReconnectDelay_ms = 2000 };

        public VectorPlusBackgroundService(ILogger<VectorPlusBackgroundService> logger)
        {
            this.logger = logger;
            AllModules = new List<IVectorPlusBehaviourModule>(ModuleHelper.ExtractModules());
        }

        public IVectorControllerPlus Controller { get; private set; }

        public List<IVectorPlusBehaviourModule> AllModules { get; private set; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this.executionStoppingToken = stoppingToken;

            Controller = CreateController();

            await Controller.ConnectAsync(controllerConfig, RetrieveRoboConfig()?.ToRobotConfiguration());
            await Controller.StartMainLoopAsync(stoppingToken);
        }

        private IVectorControllerPlus CreateController(RobotConfiguration robotConfig = null)
        {
            var controller = new VectorControllerPlus();
            controller.OnBehaviourReport += async (report) => { logger?.LogInformation(report.Description); };
            return controller;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await Controller.DisposeAsync();
            Controller = null;
        }

        private async Task DisconnectAsync(CancellationToken stoppingToken)
        {
            await Controller.DisconnectAsync();
        }

        private async Task<bool> ReconnectAsync(CancellationToken stoppingToken)
        {
            if (Controller.IsConnected)
            {
                await Controller.DisconnectAsync();
            }

            await Controller.ConnectAsync(controllerConfig, RetrieveRoboConfig()?.ToRobotConfiguration());
            return Controller.IsConnected;
        }

        public async Task<ActionResponseMessage> EraseRoboConfigAsync()
        {
            using (var db = new VectorPlusBackgroundServiceDbContext())
            {
                db.RoboConfig.RemoveRange(db.RoboConfig);
                db.SaveChanges();
            }
            return await ReconnectAsync();
        }

        public async Task<ActionResponseMessage> SetRoboConfigAsync(string robotName, string robotSerial, string email, string password, string ipOverride = null)
        {
            try
            {
                var roboConfig = new RoboConfig()
                {
                    RobotName = robotName,
                    RobotSerial = robotSerial,
                    Email = email,
                    Password = password,
                    IpOverrideStr = string.IsNullOrWhiteSpace(ipOverride) ? ipOverride : null
                };

                StoreRoboConfig(roboConfig); // store an early version with entered details

                // validations
                var validations = new List<string>();
                if (!Authentication.SerialNumberIsValid(robotSerial)) { validations.Add("Serial number invalid."); }
                if (!Authentication.RobotNameIsValid(robotName)) { validations.Add("Robot name invalid."); }
                if (validations.Count > 0)
                {
                    return ActionResponseMessage.Failure(validations);
                }

                IPAddress ipAddressOverride = null;
                if (!string.IsNullOrWhiteSpace(ipOverride)) { IPAddress.TryParse(ipOverride, out ipAddressOverride); }

                var loginResult = await Authentication.Login(robotSerial, robotName, email, password, ipAddressOverride);

                roboConfig.RobotName = loginResult.RobotName;
                roboConfig.RobotGuid = loginResult.Guid;
                roboConfig.RobotCert = loginResult.Certificate;
                roboConfig.IpOverrideStr = ipAddressOverride?.ToString();

                StoreRoboConfig(roboConfig); // store final version with cert and guid

                // if all else has succeeded, now reconnect to the robot
                return await ReconnectAsync();
            }
            catch (VectorAuthenticationException ex)
            {
                return ActionResponseMessage.Failure(ex);
            }
        }

        public async Task<ActionResponseMessage> ReconnectAsync()
        {
            bool reconnected = await ReconnectAsync(executionStoppingToken);
            return reconnected ? ActionResponseMessage.Success("Connected") : ActionResponseMessage.Failure("Failed to connect.");
        }

        public async Task<ActionResponseMessage> DisconnectAsync()
        {
            await DisconnectAsync(executionStoppingToken);
            return ActionResponseMessage.Success("Disconnected.");
        }

        public RoboConfig RetrieveRoboConfig()
        {
            using (var db = new VectorPlusBackgroundServiceDbContext())
            {
                return db.RoboConfig.SingleOrDefault();
            }
        }

        private void StoreRoboConfig(RoboConfig config)
        {
            using (var db = new VectorPlusBackgroundServiceDbContext())
            {
                db.RoboConfig.RemoveRange(db.RoboConfig);
                db.RoboConfig.Add(config);
                db.SaveChanges();
            }
        }

        public ActionResponseMessage AddModuleDLL(byte[] dll)
        {
            logger?.LogDebug("Adding new modules from DLL.");
            var modules = ModuleHelper.ExtractModules(dll, logger);
            if (modules != null)
            {
                AllModules.AddRange(modules);
                logger?.LogDebug("Modules added: " + string.Join(", ", modules.Select(m => m.Name)));

                using (var db = new VectorPlusBackgroundServiceDbContext())
                {
                    db.Modules.AddRange(modules.Select(m => ModuleConfig.From(m, dll, true)));
                    db.SaveChanges();
                }

                return ActionResponseMessage.Success("Modules added: " + string.Join(", ", modules.Select(m => m.Name)));
            }
            else
            {
                logger?.LogWarning("Module failed to load.");
                return ActionResponseMessage.Failure("Modules failed to load.");
            }
        }

        public ActionResponseMessage RemoveModule(string reference)
        {
            AllModules.RemoveAll(m => m.UniqueReference == reference);
            using (var db = new VectorPlusBackgroundServiceDbContext())
            {
                db.Modules.RemoveRange(db.Modules.Where(m => m.UniqueReference == reference));
                db.SaveChanges();
            }

            return ActionResponseMessage.Success("Removed: " + reference);
        }

        public async Task<ActionResponseMessage> ActivateBehaviourAsync(string reference)
        {
            var allBehaviours = AllModules.SelectMany(m => m.Behaviours);
            var behaviour = allBehaviours.SingleOrDefault(b => b.UniqueReference == reference);

            if (behaviour != null)
            {
                await Controller.AddBehaviourAsync(behaviour);
                return ActionResponseMessage.Success("Behaviour activated.");
            }
            else
            {
                return ActionResponseMessage.Failure("Behaviour not found.");
            }
        }

        public async Task<ActionResponseMessage> DeactivateBehaviourAsync(string reference)
        {
            await Controller.RemoveBehaviourAsync(reference);
            return ActionResponseMessage.Success("Behaviour deactivated.");
        }

    }
}
