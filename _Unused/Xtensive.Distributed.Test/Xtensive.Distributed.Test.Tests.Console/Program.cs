using System;
using System.Threading;
using Xtensive.Distributed.Test;
using System.IO;
using Xtensive.Distributed.Test.Tests.RemoteAssembly;

namespace Xtensive.Distributed.Test.Tests.Console
{
  class Program
  {
    public const string ServerUrl = "tcp://127.0.0.1:37091/Server";

    static void Main()
    {
      string target1Folder = Path.GetFullPath(@".\Target1");
      string target2Folder = Path.GetFullPath(@".\Target2");
      if (Directory.Exists(target1Folder))
        Directory.Delete(target1Folder, true);
      Directory.CreateDirectory(target1Folder);
      if (Directory.Exists(target2Folder))
        Directory.Delete(target2Folder, true);
      Directory.CreateDirectory(target2Folder);
      using (new Server(ServerUrl))
      {
        using (new Agent(ServerUrl, target1Folder)) {
          using (new Agent(ServerUrl, target2Folder)) {
            Thread.Sleep(TimeSpan.FromSeconds(3));
            Client client = new Client(ServerUrl);
            Task<ConsoleTest> task1 = client.CreateTask<ConsoleTest>();
            task1.FileManager.Upload("Xtensive.Distributed.Test.Tests.RemoteAssembly.dll", "");
            Task<ConsoleTest> task2 = client.CreateTask<ConsoleTest>();
            task2.FileManager.Upload("Xtensive.Distributed.Test.Tests.RemoteAssembly.dll", "");
            ConsoleTest test1 = task1.Start();
            test1.WriteToConsole("task1 - " + task1.Url);
            ConsoleTest test2 = task2.Start();
            test2.WriteToConsole("task2 - " + task2.Url);
            Thread.Sleep(TimeSpan.FromSeconds(20));
            task1.Kill();
            task2.Kill();
            // task.ConsoleRead -= TaskConsoleReadEvent;
          }
        }
      }
      Directory.Delete(target1Folder, true);
      Directory.Delete(target2Folder, true);
      System.Console.WriteLine("Press any key for exit.");
      System.Console.ReadKey();
    }
  }
}
