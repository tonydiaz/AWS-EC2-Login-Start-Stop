using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using AWS_EC2_Login;
using System.Configuration;

namespace AWS_EC2_Login
{
    class Program
    {
        static void Main(string[] args)
        {
            string ipAddress = "";
            List<string> instanceIds = new List<string>();
            int choice = 0;
    
            Console.WriteLine("Checking App.config file...");
            EC2_Instances.CheckAppConfig();
            Console.WriteLine("");
            
            while (choice != 3)
            {

                Console.WriteLine("Please enter 1 to start your instance or 2 to stop your instance or 3 to exit");
                choice = Int32.Parse(Console.ReadLine());

                if (choice == 1)
                {
                    Console.WriteLine("Attempting to start EC2 instance");
                    instanceIds.Add(ConfigurationManager.AppSettings["AWSInstanceID"]);
                
                    EC2_Instances.StartInstances(instanceIds);

                    ipAddress = EC2_Instances.GetIPAddress(instanceIds);
                    Console.Write("IP address for instance: " + ipAddress );

                    Console.WriteLine("");
                    Console.WriteLine("Adding local IP address to security group");
                    EC2_Instances.AddIPAddressToSecurityGroup();


                    EC2_Instances.SetupRDP(ipAddress);

                }
                else if (choice == 2)
                {
                    Console.WriteLine("Attempting to stop EC2 instance");
                
                    instanceIds.Add(ConfigurationManager.AppSettings["AWSInstanceID"]);
                    EC2_Instances.StopInstances(instanceIds);
                }
                Console.WriteLine("");
            }
            
        }
    }
}
