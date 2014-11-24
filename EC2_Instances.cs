using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon;
using Amazon.S3;
using Amazon.AWSSupport;
using Amazon.ConfigService;
using Amazon.IdentityManagement;
using Amazon.Auth;
using System.Configuration;
using MSTSCLib; //RDP
using System.Net;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace AWS_EC2_Login
{
    class EC2_Instances
    {
        private static string getExternalIp()
        {
            try
            {
                string externalIP;
                externalIP = (new WebClient()).DownloadString("http://checkip.dyndns.org/");
                externalIP = (new Regex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}"))
                             .Matches(externalIP)[0].ToString();
                return externalIP;
            }
            catch { return null; }
        }

        public static void AddIPAddressToSecurityGroup()
        {

            var ec2Client = new AmazonEC2Client();

            //Create and Initialize IpPermission Object
            var ipPermission = new IpPermission()
            {
                IpProtocol = "tcp",
                FromPort = 3389, //RDP port
                ToPort = 3389
            };  
            ipPermission.IpRanges.Add(getExternalIp() + "/32");

            //Create and initialize AuthorizeSecurityGroupIngress
            var ingressRequest = new AuthorizeSecurityGroupIngressRequest();
            ingressRequest.GroupId = ConfigurationManager.AppSettings["AWSSecurityGroupID"];
            ingressRequest.IpPermissions.Add(ipPermission);
            
            try
            {
                //Authorize Ingress
                AuthorizeSecurityGroupIngressResponse ingressResponse =
                    ec2Client.AuthorizeSecurityGroupIngress(ingressRequest);
                Console.WriteLine("IP address added to security group");
            }
            catch (Amazon.EC2.AmazonEC2Exception e)
            {
                Console.WriteLine("** IP Address already exists in the security group. **");
                //Console.WriteLine("Error:" + e);
            }
        }

        public static void CheckAppConfig()
        {
            if (ConfigurationManager.AppSettings["AWSAccessKey"] == "key" ||
                ConfigurationManager.AppSettings["AWSSecretKey"] == "secret" ||
                ConfigurationManager.AppSettings["AWSInstanceID"] == "instance")
            {
                Console.WriteLine("Please configue the App.config with your Access and Secret keys and instance ID");
                return;
            }
            Console.WriteLine("Config Complete");
        }

        public static void ConfigureAWS()
        {
            //Configuration provided in the App.config            
            //Future Method
        }

        public static void StartInstances(List<string> instanceIds)
        {
 
            var ec2Client = new AmazonEC2Client();
           
            var sir = ec2Client.StartInstances(new StartInstancesRequest(instanceIds));
            List<InstanceStateChange> statechange = sir.StartingInstances;
            
            //var request = new DescribeInstancesRequest();
            //var filter = new Filter("instance-id", instanceIds);
            //request.Filters.Add(filter);
            //var result = ec2Client.DescribeInstances(request);

            //DescribeInstanceStatusResponse instance = ec2Client.DescribeInstanceStatus(new DescribeInstanceStatusRequest());
            //InstanceState state = instance.InstanceStatuses[0].InstanceState;


            if (sir.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                Console.WriteLine("There was an error on starting the instance (HttpStatusCode): " + sir.HttpStatusCode.ToString());
            }
            if (statechange[0].CurrentState.Name == InstanceStateName.Running)
            {
                Console.WriteLine("The instance is already running");
            }
            else
            {
                foreach (InstanceStateChange item in sir.StartingInstances)
                {
                    Console.WriteLine();
                    Console.WriteLine("Started instance: " + item.InstanceId);
                    Console.WriteLine("Instance state: " + item.CurrentState.Name);
                    Console.Write("Waiting for instance to start (May take a few minutes)");
                }

                while (statechange[0].CurrentState.Name != InstanceStateName.Running)
                {
                    //Check every 3 seconds for instance state
                    System.Threading.Thread.Sleep(3000);
                    statechange = ec2Client.StartInstances(new StartInstancesRequest(instanceIds)).StartingInstances;
                    
                    //instance = ec2Client.DescribeInstanceStatus(new DescribeInstanceStatusRequest());
                    //state = instance.InstanceStatuses[0].InstanceState;
                    
                    //result = ec2Client.DescribeInstances(request);
                    
                    Console.Write(".");
                    
                }
                //Sleep for additional 10 seconds to allow IP address to be published so RDP can connect to it.  
                System.Threading.Thread.Sleep(10000);

                Console.WriteLine("");
                Console.WriteLine("Instance state: " + statechange[0].CurrentState.Name);
                //Console.WriteLine("Describe state: " + state.Name);
                //Console.WriteLine("Result: " + result.Reservations[0].Instances[0].State.Name);

            /*
                Console.Write("System Status initializing");
                var statusRequest = new DescribeInstanceStatusRequest
                {
                    InstanceIds = { instanceIds[0] }
                };
                var statusResult = ec2Client.DescribeInstanceStatus(statusRequest);
            
                while (statusResult.InstanceStatuses[0].Status.Status.Value != "ok")
                {
                    System.Threading.Thread.Sleep(3000);
                    Console.Write(".");
                    Console.WriteLine("");
                    Console.WriteLine("Status: " + statusResult.InstanceStatuses[0].Status.Status.Value);
                    statusResult = ec2Client.DescribeInstanceStatus(statusRequest);
                }
             */
            }

        }

        public static void StopInstances(List<string> instanceIds)
        {
            var ec2Client = new AmazonEC2Client();
            var stopRequest = new StopInstancesRequest() 
            {
                InstanceIds = instanceIds
            };

            var stopResponse = ec2Client.StopInstances(stopRequest);
            List<InstanceStateChange> statechange = stopResponse.StoppingInstances;

            foreach (InstanceStateChange item in stopResponse.StoppingInstances)
            {
                Console.WriteLine();
                Console.WriteLine("Stopped instance: " + item.InstanceId);
                Console.WriteLine("Instance state: " + item.CurrentState.Name);
                Console.Write("Stopping Instance");
            }

            while (statechange[0].CurrentState.Name != InstanceStateName.Stopped)
            {
                //Check every 1 second for instance state
                System.Threading.Thread.Sleep(3000);
                statechange = ec2Client.StopInstances(stopRequest).StoppingInstances;
                Console.Write(".");
            }

            Console.Write("\n");
            Console.WriteLine("Instance stopped");
            
        }

        public static string  GetIPAddress(List<string> instanceIds)
        {
            
            string ipAddress = "";
            var ec2client = new AmazonEC2Client();
            var describeRequest = new DescribeInstancesRequest() 
            {
                InstanceIds = instanceIds
            };

            var response = ec2client.DescribeInstances(describeRequest);
            
            List<Reservation> reser =  response.DescribeInstancesResult.Reservations;
            ipAddress = reser[0].Instances[0].PublicIpAddress.ToString();
            
            return ipAddress;
        }

        public static void SetupRDP(string ipAddress)
        {
            
            Process rdcProcess = new Process();
            rdcProcess.StartInfo.FileName = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\cmdkey.exe");
            rdcProcess.StartInfo.Arguments =
                "/generic:TERMSRV/" + ipAddress +
                " /user:" + ConfigurationManager.AppSettings["RDPDomain"] + "\\" + ConfigurationManager.AppSettings["RDPUserName"];
            Console.WriteLine("RDP:" + rdcProcess.StartInfo.Arguments);
            
            rdcProcess.Start();

            rdcProcess.StartInfo.FileName = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\mstsc.exe");
            rdcProcess.StartInfo.Arguments = "/v " + ipAddress; // ip or name of computer to connect
            rdcProcess.Start();
            
        }



    }
}
