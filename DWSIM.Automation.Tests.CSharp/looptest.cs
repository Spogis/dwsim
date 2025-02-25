﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

public class LoopTest
{
    [STAThread]
    static void Main()
    {

        Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

        //create automation manager

        var interf = new DWSIM.Automation.Automation3();
        for (int i = 0; i < 100; i++)
        {
            var sw = new Stopwatch();
            sw.Start();
            var sim = interf.LoadFlowsheet("C:\\Users\\danie\\Downloads\\capeopen.dwxmz");
            sim.SetMessageListener((s, mt) => Console.WriteLine(s));
            interf.CalculateFlowsheet2(sim);
            //unvell.ReoGrid.IWorkbook  ssheet = (unvell.ReoGrid.IWorkbook)sim.GetSpreadsheetObject();
            //var val1 = ssheet.Worksheets[0].Cells["C4"].Data;
            //var val2 = ssheet.Worksheets[0].Cells["C5"].Data;
            //var val3 = ssheet.Worksheets[0].Cells["E4"].Data;
            //ssheet.Worksheets[0].Recalculate();
            sim.ReleaseResources();
            sim = null;
            Console.WriteLine(String.Format("Finished in {0} ms.", sw.ElapsedMilliseconds));
        }
        interf.ReleaseResources();

        Console.ReadLine();
    }

}