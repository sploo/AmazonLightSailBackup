using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;



// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace LightSailBackup
{
    public class Function
    {

        private AmazonLightsailClient m_Client = null;
        private AmazonLightsailConfig m_Config = null;
        private Configuration m_Input = null;
        private ILambdaContext m_Context = null;
        private List<InstanceSnapshot> m_SnapshotList = null;

        /// <summary>
        /// A simple function that backup LightSail instance snapshot
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<string> FunctionHandler(Configuration input, ILambdaContext context)
        {            
            this.m_Input = input;
            this.m_Context = context;

            this.m_SnapshotList = new List<InstanceSnapshot>();

            this.m_Config = new AmazonLightsailConfig();
            this.m_Config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(this.m_Input.Region);            

            this.m_Client = new AmazonLightsailClient(this.m_Config);

            GetInstancesRequest _instancesRequest = new GetInstancesRequest();
            GetInstancesResponse _instancesResponse = await this.m_Client.GetInstancesAsync(_instancesRequest);

            await this.Backup(_instancesResponse);
            await this.LoadAllSnapshots();
            await this.CleanUp(_instancesResponse);

            return this.m_Input.Name + " completed successfully - " + context.AwsRequestId;
        }

        public async Task LoadAllSnapshots(string PageToken = "")
        {
            GetInstanceSnapshotsRequest _snapshotsRequest = new GetInstanceSnapshotsRequest();

            if (PageToken == "")
            {
                this.m_SnapshotList.Clear();                
            }
            else
            {
                _snapshotsRequest.PageToken = PageToken;
            }

            GetInstanceSnapshotsResponse _snapshotsRepsonse = await this.m_Client.GetInstanceSnapshotsAsync(_snapshotsRequest);

            this.m_SnapshotList.AddRange(_snapshotsRepsonse.InstanceSnapshots);
            if (_snapshotsRepsonse.NextPageToken != null)
            {
                await this.LoadAllSnapshots(_snapshotsRepsonse.NextPageToken);
            }
        }

        public async Task Backup(GetInstancesResponse Response)
        {
            this.m_Context.Logger.LogLine("Intializing " + this.m_Input.Name + " backup procedure: " + Response.Instances.Count.ToString () + " instance");

            foreach (Instance _instance in Response.Instances)
            {
                this.m_Context.Logger.LogLine("Backuping - " + _instance.Name);
                CreateInstanceSnapshotRequest _instanceSnapshotRequest = new CreateInstanceSnapshotRequest();
                _instanceSnapshotRequest.InstanceName = _instance.Name;
                _instanceSnapshotRequest.InstanceSnapshotName = String.Format("{0}_{1}_{2:yyyyMMdd_hhmmss}", this.m_Input.Name, _instance.Name, DateTime.Now);
                CreateInstanceSnapshotResponse _instanceSnapshotResponse = await this.m_Client.CreateInstanceSnapshotAsync(_instanceSnapshotRequest);
                this.m_Context.Logger.LogLine("Backup succesfully - " + _instance.Name);                
            }
            this.m_Context.Logger.LogLine(this.m_Input.Name + " backup procedure completed successfully!");
        }

        private async Task CleanUp(GetInstancesResponse Response)
        {
            this.m_Context.Logger.LogLine("Intializing " + this.m_Input.Name + " clean-up procedure: " + Response.Instances.Count.ToString() + " instance");
            foreach (Instance _instance in Response.Instances)
            {
                this.m_Context.Logger.LogLine("Cleaning - " + _instance.Name);
                await this.DeleteExpiredSnapshot(_instance.Name);
                this.m_Context.Logger.LogLine("Cleaning successfully - " + _instance.Name);
            }
            this.m_Context.Logger.LogLine(this.m_Input.Name + " clean-up procedure completed successfully!");
        }        

        public async Task DeleteExpiredSnapshot(string Name)
        {
            List<InstanceSnapshot> _expiredSnapshots = this.getExpiredSnapshotsByInstance(Name);
            if(_expiredSnapshots != null)
            {
                foreach (InstanceSnapshot _expiredSnapshot in _expiredSnapshots)
                {
                    this.m_Context.Logger.LogLine("Removing snapshot - " + _expiredSnapshot.Name);
                    DeleteInstanceSnapshotRequest _request = new DeleteInstanceSnapshotRequest();
                    _request.InstanceSnapshotName = _expiredSnapshot.Name;
                    await this.m_Client.DeleteInstanceSnapshotAsync(_request);
                    this.m_Context.Logger.LogLine("Removing snapshot succesfully - " + _expiredSnapshot.Name);
                }
            }           
        }

        public List<InstanceSnapshot> getExpiredSnapshotsByInstance(string Name)
        {
            List<InstanceSnapshot> _expiredSnapshots = null;

            if(this.m_Input.RetentionPolicy == Configuration.RetentionPolicyType.Period)
            {
                _expiredSnapshots = this.m_SnapshotList.Where(w => w.FromInstanceName == Name && w.CreatedAt.AddDays(this.m_Input.RetentionLimit) < DateTime.Now).OrderByDescending(o => o.CreatedAt).ToList ();
            }
            else
            {
                _expiredSnapshots = this.m_SnapshotList.Where(w => w.FromInstanceName == Name).OrderByDescending(o => o.CreatedAt).Skip(this.m_Input.RetentionLimit).ToList();
            }

            return _expiredSnapshots;                        
        }        
    }
}
