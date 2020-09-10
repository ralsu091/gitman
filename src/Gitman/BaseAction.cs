using System;
using Octokit;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace gitman
{
    public abstract class BaseAction
    {
        public IGitHubClient Client { get; set; }

        public abstract Task Do();

        protected void l(string msgs, int tab = 0) => Console.WriteLine(new String('\t', tab) + msgs);
        
        protected string Dump(IEnumerable<string> list) => string.Join(", ", list);
    }
}