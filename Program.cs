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
            client.Credentials = new Credentials("joostschriek", "ahahahah");
            
            await new Collaborators("developers") { Client = client }.DoForAll();
            await new Collaborators("bravo") { Client = client }.DoForAll();
            await new Collaborators("alpha") { Client = client }.DoForAll();
            await new Protection() { Client = client }.DoForAll();
        }
    }
}