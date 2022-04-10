﻿using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using LoggingAbstractions;

using Serilog;
using Serilog.Debugging;
using Serilog.Formatting.Compact;

namespace Sample
{
    class Program
    {
        static async Task Main()
        {
            var fs = new FileStream("selflog.txt", FileMode.Append, FileAccess.Write, FileShare.Read, 4096, FileOptions.Asynchronous);
            var writer = new StreamWriter(fs, Encoding.UTF8) { AutoFlush = false };
            async void Output(string line) => await writer.WriteLineAsync(line);
            SelfLog.Enable(Output);


            using var serilog = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(new CompactJsonFormatter(), "log.txt")
                .Destructure.ByTransforming<UserData>(u => new { u.Id, u.Username })
                .CreateLogger();


            var abstraction = (ILog)(typeof(LoggingAbstractions.Serilog.Log).GetConstructor(new[] { typeof(ILogger) })?.Invoke(new object[] { serilog }) ?? throw new NullReferenceException());


            //LogHelper.InfoFormat(abstraction, (IClientRequestInfo?)null, "Hello, {Name}!", "Mike", "123");
            //abstraction.InfoFormat((IClientRequestInfo?)null, "Hello, {Name}!", "Mike", "123");
            //abstraction.Info((object) (IClientRequestInfo?)null, (Exception) "Hello, {Name}!");

            //abstraction.InfoFormat("Evaluated as {2} [Sym={0} Prop={1}] Eval={2}", "Q", "W", "E");

            abstraction.InfoFormat("Hello, {Name}{YYY}!", "Mike", 1);

            //abstraction.InfoFormat("Hello, {Name}!", "Mike", "123");
            //abstraction.InfoFormat("Hello, {Name}{ABC}{SSS}!", "Mike", "333");

            //serilog.Information("Hello, {Name}!", "Mike", "gfgfgf");
            //serilog.Information("Hello, {Name}{ABC}{SSS}!", "Mike", "gfgfgf");

            serilog.Information("Hello, {Name}!", "Mike");
            serilog.Information("Updating profile for {@User}", new UserData("123456", "Mike", "short", "pass"));
            serilog.Information("Updating profile for {User}", new UserData("123456", "Mike", "short", "pass"));

            await writer.FlushAsync();
        }

        class UserData
        {
            public string Id { get; set; }
            public string Username { get; set; }
            public string Biography { get; set; }
            public string NewPassword { get; set; }

            public UserData(string id, string username, string biography, string newPassword)
            {
                Id = id;
                Username = username;
                Biography = biography;
                NewPassword = newPassword;
            }

            public override string ToString()
            {
                return Username;
            }
        }
    }
}