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
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Security.Permissions;

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
            case "cmd":
                openCmdExperiment();
                break;
            case "ver":
                GetFileVersion();
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


    public static void openCmdExperiment()
    {
        //Vssadmin Delete Shadows /For=C: /Oldest
        //https://www.winhelponline.com/blog/how-to-delete-system-restore-points-windows/
        //
        //https://docs.microsoft.com/en-us/troubleshoot/windows-server/backup-and-storage/automating-disk-cleanup-tool
        // Define variables to track the peak
        // memory usage of the process.
        //long peakPagedMem = 0,
        //     peakWorkingSet = 0,
        //    peakVirtualMem = 0;

        ProcessStartInfo ProcessInfo;
        Process Process;

        //ProcessInfo = new ProcessStartInfo("cmd.exe", "/K " + "Vssadmin Delete Shadows /For=C: /Oldest");
        ProcessInfo = new ProcessStartInfo("cmd.exe", "/K " + "Vssadmin Delete Shadows /For=C: /Oldest");
        ProcessInfo.CreateNoWindow = true;
        ProcessInfo.UseShellExecute = true;

        Process = Process.Start(ProcessInfo);

        // Start the process.
        //using (Process myProcess = Process.Start("notepad.exe"))
        //{
            // Display the process statistics until
            // the user closes the program.
            //https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.exitcode?redirectedfrom=MSDN&view=net-5.0#System_Diagnostics_Process_ExitCode
        //}
    }

    /*
    public static void AdminRelauncher()
    {
        if (!IsRunAsAdmin())
        {
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.UseShellExecute = true;
            proc.WorkingDirectory = Environment.CurrentDirectory;
            proc.FileName = Assembly.GetEntryAssembly().CodeBase;

            proc.Verb = "runas";

            try
            {
                Process.Start(proc);
                //Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                Console.WriteLine("This program must be run as an administrator! \n\n" + ex.ToString());
            }
        }
    }

    public bool IsRunAsAdmin()
    {
        WindowsIdentity id = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(id);

       return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
    */

    public static void GetFileVersion()
    {
        // Get the file version for the notepad.
        FileVersionInfo myFileVersionInfo = FileVersionInfo.GetVersionInfo(@"C:\Program Files\Bonjour\mDNSResponder.exe");

        // Print the file name and version number.
        String text = "File: " + myFileVersionInfo.FileDescription + '\n' +
        "Version number: " + myFileVersionInfo.FileVersion;
        Console.WriteLine(text);



        //C: \Users\Abhisek Pramanik\source\repos\ConsoleApp - Diagnosis > dotnet run--project ConsoleApp-Diagnosis ver
        //File: Bonjour Service
        //Version number: 3,0,0,10
     }

}
