namespace gitman {
    public static class Config
        {
            public static class Github {
                public static string User { get; set; }
                public static string Token { get; set; }
                public static string Org {get; set;} = "sectigo-eng";

                public static new string ToString() => $"Github[user={User} token={!string.IsNullOrEmpty(Token)} Org={Org}]";
            }
            public static bool DryRun { get; set; } = true;
            public static bool Help { get; set; }

            public static bool Validate() => !string.IsNullOrEmpty(Github.User) && !string.IsNullOrEmpty(Github.Token);

            public static new string ToString() => $"{Github.ToString()} DryRun={DryRun} Help={Help}";
        }
}