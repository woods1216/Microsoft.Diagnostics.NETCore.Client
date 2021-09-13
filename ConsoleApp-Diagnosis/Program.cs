//https://github.com/dotnet/diagnostics/blob/main/documentation/design-docs/diagnostics-client-library.md
//Abhisek Pramanik
//https://www.youtube.com/watch?v=Rei6d9nKaFQ


//>dotnet add <ProjectName ConsoleApp-Diagnosis> package Microsoft.Diagnostics.NETCore.Client --version 0.2.236902
using Microsoft.Diagnostics.NETCore.Client;
//>dotnet add <ProjectName ConsoleApp-Diagnosis> package Microsoft.Diagnostics.Tracing --version ??? see website
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.EventPipe;
using Microsoft.Diagnostics.Tracing.Parsers;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using Process = System.Diagnostics.Process;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;


public class RuntimeGCEventsPrinter

{

    public static void Main(string[] args)
    {
        switch (args[0])
        {
            case "ps":
                PrintProcessStatus();
                break;
            case "printEvent":
                PrintRuntimeGCEvents(Int32.Parse(args[1]));
                break;
            case "printDump":
                TriggerCoreDump(Int32.Parse(args[1]));
                break;
            case "trace":
                TraceProcessForDuration(Int32.Parse(args[1]), Int32.Parse(args[2]));
                break;

        }

    }

   

public static void TraceProcessForDuration(int processId, int duration)
{
    var cpuProviders = new List<EventPipeProvider>()
    {
        new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, (long)ClrTraceEventParser.Keywords.Default),
        new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Informational, (long)ClrTraceEventParser.Keywords.None)
    };
    var client = new DiagnosticsClient(processId);
    using (var traceSession = client.StartEventPipeSession(cpuProviders))
    {
        Task copyTask = Task.Run(async () =>
        {
            using (FileStream fs = new FileStream("trace.log", FileMode.Create, FileAccess.Write))
            {
                await traceSession.EventStream.CopyToAsync(fs);
                //use PerfView to view logs -- Abhisek 
            }
        });

        copyTask.Wait(duration * 1000);
        traceSession.Stop();
    }
}


public static void TriggerCoreDump(int processId)
    {
        var client = new DiagnosticsClient(processId);
        client.WriteDump(DumpType.Normal, "C:\\", false);
    }


public static void PrintRuntimeGCEvents(int processId)
    {
        var providers = new List<EventPipeProvider>()
        {
            new EventPipeProvider("Microsoft-Windows-DotNETRuntime",
                EventLevel.Informational, (long)ClrTraceEventParser.Keywords.GC)
        };

        var client = new DiagnosticsClient(processId);
        using (EventPipeSession session = client.StartEventPipeSession(providers, false))
        {
            var source = new EventPipeEventSource(session.EventStream);

            source.Clr.All += (TraceEvent obj) => Console.WriteLine(obj.ToString());

            try
            {
                source.Process();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error encountered while processing events");
                Console.WriteLine(e.ToString());
            }
        }
    }


    public static void PrintProcessStatus()
    {
        var processes = DiagnosticsClient.GetPublishedProcesses()
            .Select(Process.GetProcessById)
            .Where(process => process != null);

        foreach (var process in processes)
        {
            Console.WriteLine($"{process.ProcessName} {process.Id}");
        }
    }

}