// Copyright (C) 2007 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Gamzov
// Created:    2007.10.24

using System;
using System.IO;
using System.Threading;
using NUnit.Framework;
using Xtensive.Distributed.Test.Tests.RemoteAssembly;

namespace Xtensive.Distributed.Test.Tests
{
  [TestFixture]
  public class ClientDispose
  {
    public const string ServerUrl = "tcp://127.0.0.1:37091/Server";

    private bool errorStringReaded;
    private bool outStringReaded;
    private readonly string targetFolder = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "Target");


    [Test]
    public void Dispose()
    {
      if (Directory.Exists(targetFolder))
        Directory.Delete(targetFolder, true);
      Directory.CreateDirectory(targetFolder);
      using (new Server(ServerUrl))
      {
        using (Agent agent = new Agent(ServerUrl, targetFolder))
        {
          Thread.Sleep(TimeSpan.FromSeconds(3));
          Client client = new Client(ServerUrl);
          AgentInfo[] availableAgents = client.Agents;
          Assert.Greater(availableAgents.Length, 0);
          Task<ConsoleTest> task = client.CreateTask<ConsoleTest>();
          task.FileManager.Upload("Xtensive.Distributed.Test.Tests.RemoteAssembly.dll", "");
          task.ConsoleRead += TaskConsoleReadEvent;
          task.Start();
          Assert.AreEqual(1, agent.Tasks.Length);
          client.Dispose();
          Thread.Sleep((int)Agent.ClientKeepAliveTimeout.TotalMilliseconds*3);
          Console.WriteLine("DISPOSE AGENT etc");
          Assert.AreEqual(0, agent.Tasks.Length);
          // task.Kill();
          // task.ConsoleRead -= TaskConsoleReadEvent;
        }
      }
      Directory.Delete(targetFolder, true);
      Assert.IsTrue(errorStringReaded);
      Assert.IsTrue(outStringReaded);
    }

    private void TaskConsoleReadEvent(object sender, ConsoleReadEventArgs e)
    {
      if (e.Message == ConsoleTest.ConsoleErrorString && e.IsError)
        errorStringReaded = true;
      if (e.Message == ConsoleTest.ConsoleOutputString && !e.IsError)
        outStringReaded = true;
    }
  }
}