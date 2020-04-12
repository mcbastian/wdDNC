using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using wdDB.Model;

namespace wdWorker
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Worker starting...");
            CultureInfo culture = new CultureInfo("en-US");
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            Console.WriteLine("Output to: " + config["outDir"]);
	    Console.WriteLine("Current Dir: "+Directory.GetCurrentDirectory());
	    Console.WriteLine(config.ToString());
            Model1Factory dbf = new Model1Factory();
            wdDBModel db = dbf.CreateDbContext(new string[] { config["DefaultConnection"] });
            Console.WriteLine("Connected to DB...");
            while (true)
            {
                var jobs = await db.Job.Where(e => e.Status == 0).OrderBy(e => e.Id).ToListAsync();
                if (jobs.Count == 0)
                {
                    Task.Delay(20000).Wait();
                    continue;
                }
                Console.WriteLine("got {0} jobs...", jobs.Count);
                foreach (Job j in jobs)
                {
                    j.Status = 1;
                    db.Update(j);
                    var dbsave = db.SaveChangesAsync();
                    string outDir = config["outDir"] + "/" + j.Id.ToString();
                    string param = "";
                    param += "-D " + outDir;
                    param += " -b " + config["astroConfig"];
                    param += " -o " + j.Id.ToString();
                    param += " --wcs " + outDir + "/wcs.fits";
                    param += " --corr " + outDir + "/corr.fits";
                    param += " --rdls " + outDir + "/rdls.fits";
                    param += " --no-plots ";
                    param += " -S " + outDir + "/solved";
                    param += " -l " + config["timeLimit"];
                    if (j.Scale_units.Length > 0) param += " --scale-units " + j.Scale_units;
                    if (j.Scale_type == "ul")
                    {
                        param += " --scale-high " + j.Scale_upper.ToString(culture);
                        param += " --scale-low " + j.Scale_lower.ToString(culture);
                    }
                    param += " --ra " + j.Center_ra.ToString(culture);
                    param += " --dec " + j.Center_dec.ToString(culture);
                    param += " --radius " + j.Radius.ToString(culture);

                    if (j.DownsampleFactor >= 2.0) param += " --downsample " + j.DownsampleFactor.ToString(culture);
                    param += " --tweak-order 2 ";
                    //                    param += " --crpix-center " + j.CrpixCenter.ToString();
                    //                    param += " --parity " + (j.Parity == 0 ? "pos" : "neg");
                    param += " --temp-dir " + outDir + "/tmp";
                    param += " " + j.OFilename;
                    Console.WriteLine("Param for solver: " + param);
                    Directory.CreateDirectory(outDir + "/tmp");
                    using (var process = new Process())
                    {
                        process.StartInfo.FileName = @"/usr/local/astrometry/bin/solve-field"; // relative path. absolute path works too.
                        process.StartInfo.Arguments = param;
                        process.StartInfo.CreateNoWindow = true;
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.RedirectStandardError = true;

                        process.OutputDataReceived += (sender, data) => Console.WriteLine(data.Data);
                        process.ErrorDataReceived += (sender, data) => Console.WriteLine(data.Data);
                        Console.WriteLine("--- starting solve-field");
                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                        process.WaitForExit();     // (optional) wait up to 10 seconds
                        Console.WriteLine($"--- solve field ended");
                    }
                    await dbsave;
                    if (!File.Exists(outDir + "/solved"))
                    {
                        Console.WriteLine("Solving failed!");
                        j.Status = 3;
                        db.Update(j);
                        await db.SaveChangesAsync();
                        continue;
                    }
                    using (var process = new Process())
                    {
                        process.StartInfo.FileName = config["scriptDir"]+@"/unfits.py"; // relative path. absolute path works too.
                        process.StartInfo.Arguments = outDir + "/wcs.fits " + outDir + "/wcs.txt";
                        process.StartInfo.CreateNoWindow = true;
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.RedirectStandardError = true;

                        process.OutputDataReceived += (sender, data) => Console.WriteLine(data.Data);
                        process.ErrorDataReceived += (sender, data) => Console.WriteLine(data.Data);
                        Console.WriteLine("--- starting unfits.py ");
                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                        process.WaitForExit();     // (optional) wait up to 10 seconds
                        Console.WriteLine($"--- unfits.py ended");
                    }
                    string fits = File.ReadAllText(outDir + "/wcs.txt");
                    var calc = new Calculator();
                    calc.Do(fits);
                    j.Result_dec = calc.Dec;
                    j.Result_ra = calc.Ra;
                    j.Result_radius = calc.Radius;
                    j.Result_pixscale = calc.Pixscale;
                    j.Result_parity = calc.Parity;
                    j.Result_orientation = calc.Orientation;
                    j.Status = 2;
                    j.Finished = DateTime.Now;
                    db.Update(j);
                    await db.SaveChangesAsync();
                    Console.WriteLine("Solving job {0} succeded! Database updated", j.Id);
                }
            }
        }

    }
}
