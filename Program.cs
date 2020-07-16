using System;
using Octokit;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace gitman
{
    class Program
    {
        private static GitHubClient client;
        private static bool DryRun = true;

        static async Task Main(string[] args)
        {
            client = new GitHubClient(new ProductHeaderValue("SuperMassiveCLI"));
            client.Credentials = new Credentials("joostschriek", "haha");
            
            Console.WriteLine("Checking merge setting");
            await new Merging(squash: true) { Client = client }.DoForAll();
            
            Console.WriteLine("\n\nChecking team collaborators");
            await new Collaborators("developers") { Client = client }.DoForAll();
            await new Collaborators("admins", Permission.Admin) { Client = client }.DoForAll();
            // await new Collaborators("bravo") { Client = client }.DoForAll();
            // await new Collaborators("alpha") { Client = client }.DoForAll();
            
            Console.WriteLine("\n\nChecking branch protections");
            await new Protection() { Client = client }.DoForAll();
        }
    }
}