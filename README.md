AWS-EC2-Login-Start-Stop 
========================


This application will login to your ec2 instance start it up, determine the IP address of the EC2 instance, determine your IP address and add it to a security group you specify and launch Remote Desktop (RDP) to log in to the account. The application will also shutdown an instance if you would like.


App.config - File to add your AWS parameters (Access Key, Secret Key, EC2 instance ID, AWS Region of EC2 instance, Security group ID and RDP parameters)

Program.cs - Main method for the command-line applciation.

EC2_Instances.cs  - Methods to Start, Stop an EC2 instance. Retrieve EC2 instance IP address, and your own external IP address to add to the security group. 

(Amazon Web Services)
