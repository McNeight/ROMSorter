﻿using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;
using SharpCompress.Writers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RomDatabase5
{
    public static class CoreFunctions
    {
        public static bool MoveMissedPatches = false;
        public static void DetectDupes(IProgress<string> p, string path)
        {
            //Detect duplicates.
            Dictionary<string, string> crcHashes = new Dictionary<string, string>();
            Dictionary<string, string> md5Hashes = new Dictionary<string, string>();
            Dictionary<string, string> sha1Hashes = new Dictionary<string, string>();
            bool foundDupe = false;
            Hasher h = new Hasher();
            Directory.CreateDirectory(path + "/Duplicates");
            foreach (var file in Directory.EnumerateFiles(path))
            {
                var filename = Path.GetFileNameWithoutExtension(file);
                //TODO: check zipped data separately? Or assume stuff was run to make files consistent.
                p.Report(Path.GetFileName(file));
                HashResults results = h.HashFileAtPath(file);

                if (crcHashes.ContainsKey(results.crc) && md5Hashes.ContainsKey(results.md5) && sha1Hashes.ContainsKey(results.sha1))
                {
                    // this is a dupe, we hit on all 3 hashes.
                    foundDupe = true;
                    var origName = crcHashes[results.crc];
                    var dirName = path + "/Duplicates/" + origName.Replace("(", "").Replace(")", "").Trim();
                    Directory.CreateDirectory(dirName);
                    File.Move(file, dirName + "/" + Path.GetFileName(file));
                }

                crcHashes.TryAdd(results.crc, filename);
                md5Hashes.TryAdd(results.md5, filename);
                sha1Hashes.TryAdd(results.sha1, filename);
            }

            if (foundDupe)
                p.Report("Completed, duplicates found and moved.");
            else
                p.Report("Completed, no duplicates.");
        }

        public static void UnzipLogic(IProgress<string> progress, string path)
        {
            var files = Directory.EnumerateFiles(path).ToList();
            foreach (var file in files)
            {
                progress.Report(file);
                switch (Path.GetExtension(file.ToLower()))
                {
                    case ".zip":
                    case ".rar":
                    case ".gz":
                    case ".gzip":
                    case ".tar":
                    case ".7z":
                        //case ".lz":
                        try
                        {
                            using (var mmf = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateFromFile(file))
                            using (var fileData = mmf.CreateViewStream())
                            {
                                using (var existingZip = SharpCompress.Archives.ArchiveFactory.Open(fileData))
                                {
                                    if (existingZip != null)
                                    {
                                        foreach (var e in existingZip.Entries)
                                            e.WriteToDirectory(path);
                                    }
                                }
                            }
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                        }
                        break;
                    case ".lz": //SharpCompress does not have a setup to nicely handle .tar.lz files internally.
                        using (var mmf = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateFromFile(file))
                        using (var fileData = mmf.CreateViewStream())
                        {
                            var zf = ReaderFactory.Open(fileData);

                            //This block works, but needs more disk space since it unzips the .tar and then unzips the tar's contents.
                            //Might need to manually set this up to read an lzma stream. This doesn't nicely chain together, so I need the temp file.
                            var outerStream = new SharpCompress.Compressors.LZMA.LZipStream(fileData, SharpCompress.Compressors.CompressionMode.Decompress);
                            var testFileOut = File.Create(path + "/temp.tar");
                            outerStream.CopyTo(testFileOut);
                            testFileOut.Close();
                            outerStream.Dispose();

                            var innerStream = SharpCompress.Archives.Tar.TarArchive.Open(testFileOut);
                            var reader = innerStream.ExtractAllEntries();
                            reader.WriteAllToDirectory(path);
                            //using (var existingZip = SharpCompress.Archives.Tar.TarArchive.Open(fileData))
                            //foreach(var  e in innerStream.)
                            //{ 
                                
                                //e.WriteToDirectory(path); 
                            //}
                            innerStream.Dispose();

                            File.Delete(path + "/temp.tar");

                        }
                        break;
                    case ".7zother":
                        try
                        {
                            using (var mmf = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateFromFile(file))
                            using (var fileData = mmf.CreateViewStream())
                            {
                                using (var existingZip = SharpCompress.Archives.ArchiveFactory.Open(fileData))
                                {
                                    if (existingZip != null)
                                    {
                                        var reader = existingZip.ExtractAllEntries();
                                        reader.WriteAllToDirectory(path);
                                    }
                                }
                            }
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                        }
                        break;
                }
            }
            progress.Report("Complete");
        }

        public static void ZipLogic(IProgress<string> progress, string path)
        {
            var files = Directory.EnumerateFiles(path).ToList();
            int count = 1;
            foreach (var file in files)
            {
                progress.Report(count + "/" + files.Count() + ":" + Path.GetFileName(file));
                string tempfilename = Path.GetDirectoryName(file) + "/" + Path.GetFileNameWithoutExtension(file) + ".zip-temp";

                var zfs = File.Create(tempfilename);
                var zf = new ZipArchive(zfs, ZipArchiveMode.Create);
                switch (Path.GetExtension(file.ToLower()))
                {
                    case ".zip":
                    case ".rar":
                    case ".gz":
                    case ".gzip":
                    case ".tar":
                    case ".7z":
                        using (var mmf = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateFromFile(file))
                        using (var fileData = mmf.CreateViewStream())
                        {
                            using (var existingZip = SharpCompress.Archives.ArchiveFactory.Open(fileData))
                                Helpers.RezipFromArchive(existingZip, zf);
                        }
                        break;
                    case ".lz":
                        using (var mmf = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateFromFile(file))
                        using (var fileData = mmf.CreateViewStream())
                        {
                            var outerStream = new SharpCompress.Compressors.LZMA.LZipStream(fileData, SharpCompress.Compressors.CompressionMode.Decompress);
                            string tempfile = path + "/temp.tar";
                            var testFileOut = File.Create(tempfile);
                            outerStream.CopyTo(testFileOut);
                            testFileOut.Close();
                            outerStream.Dispose();

                            var stream2 = File.OpenRead(tempfile);
                            var innerStream = SharpCompress.Archives.Tar.TarArchive.Open(stream2);
                            var reader = innerStream.ExtractAllEntries();
                            reader.WriteAllToDirectory(path);
                            reader.Dispose();

                            //Might need to manually set this up to read an lzma stream.
                            //var outerStream = new SharpCompress.Compressors.LZMA.LZipStream(fileData, SharpCompress.Compressors.CompressionMode.Decompress);
                            //var innerStream = SharpCompress.Archives.Tar.TarArchive.Open(outerStream);
                            //using (var existingZip = SharpCompress.Archives.Tar.TarArchive.Open(fileData))
                            //Helpers.RezipFromArchive(innerStream, zf);
                            innerStream.Dispose();
                            stream2.Close(); stream2.Dispose();
                            File.Delete(tempfile);
                        }
                        break;
                    default:
                        zf.CreateEntryFromFile(file, Path.GetFileName(file));
                        break;
                }
                zf.Dispose();
                zfs.Close(); zfs.Dispose();
                File.Move(tempfilename, tempfilename.Replace("-temp", ""), true);
                if (!file.EndsWith(".zip")) //we just overwrote this file, don't remove it.
                {
                    File.Delete(file);
                }
                count++;
            }
            progress.Report("Complete");
        }

        public static void LZipLogic(IProgress<string> progress, string path)
        {
            var files = Directory.EnumerateFiles(path).ToList();
            int count = 1;
            foreach (var file in files)
            {
                progress.Report(count + "/" + files.Count() + ":" + Path.GetFileName(file));

                string tempfilename = Path.GetDirectoryName(file) + "/" + Path.GetFileNameWithoutExtension(file) + ".lzmazip-temp";

                var zfs = File.Create(tempfilename);
                var zf = WriterFactory.Open(zfs, SharpCompress.Common.ArchiveType.Zip, new WriterOptions(SharpCompress.Common.CompressionType.LZMA)); //LZMA does 
                switch (Path.GetExtension(file.ToLower()))
                {
                    case ".zip":
                    case ".rar":
                    case ".gz":
                    case ".gzip":
                    case ".tar":
                    case ".7z":
                        using (var mmf = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateFromFile(file))
                        using (var fileData = mmf.CreateViewStream())
                        {
                            using (var existingZip = SharpCompress.Archives.ArchiveFactory.Open(fileData))
                                Helpers.RezipFromArchive(existingZip, zf);
                        }
                        break;
                    case ".lz":
                        //Already done under this method, skip it.
                        break;
                    default:
                        zf.Write(file.Replace(path, ""), new FileInfo(file));
                        break;
                }
                zf.Dispose();
                zfs.Close(); zfs.Dispose();
                if (!file.EndsWith(".lzmazip")) //if this was an lzmazip file, we skipped it.
                {
                    File.Move(tempfilename, tempfilename.Replace("-temp", ""), true);
                    File.Delete(file);
                }
                count++;
            }
            progress.Report("Complete");
        }

        public static void Catalog(IProgress<string> progress, string path)
        {
            //Hash all files in directory, write results to a tab-separated values file 
            FileStream fs = File.OpenWrite(path + "/catalog.tsv");
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine("name\tmd5\tsha1\tcrc\tsize");
            Hasher hasher = new Hasher();
            var files = Directory.EnumerateFiles(path).Where(f => Path.GetFileName(f) != "catalog.tsv").ToList();
            foreach (var file in files)
            {
                progress.Report(file);
                var hashes = hasher.HashFileAtPath(file);
                sw.WriteLine(Path.GetFileName(file) + "\t" + hashes.md5 + "\t" + hashes.sha1 + "\t" + hashes.crc + "\t" + hashes.size);
            }
            sw.Close(); sw.Dispose(); fs.Close(); fs.Dispose();
            progress.Report("Complete");
        }

        public static void Verify(IProgress<string> progress, string path)
        {
            bool alert = false;
            var files = File.ReadAllLines(path + "/catalog.tsv");
            var foundfiles = new List<string>();
            var filesInFolder = Directory.EnumerateFiles(path).Where(s => Path.GetFileName(s) != "catalog.tsv").Select(s => Path.GetFileName(s)).ToList();
            Hasher hasher = new Hasher();
            foreach (var file in files.Skip(1))  //ignore header row.
            {
                string[] vals = file.Split("\t");
                foundfiles.Add(vals[0]);
                progress.Report(vals[0]);
                try
                {
                    var hashes = hasher.HashFileAtPath(file);
                    if (vals[1] == hashes.md5 && vals[2] == hashes.sha1 && vals[3] == hashes.crc) //intentionally leaving size out, despite recording it.
                    {
                        continue;
                    }
                    else
                    {
                        alert = true;
                        File.AppendAllText(path + "/report.txt", vals[0] + " did not match:" + vals[1] + "|" + hashes.md5 + " " + vals[2] + "|" + hashes.sha1 + " " + vals[3] + "|" + hashes.crc + " " + vals[4] + "|" + hashes.size);
                    }
                }
                catch (Exception ex)
                {
                    alert = true;
                    File.AppendAllText(path + "/report.txt", "Error checking on " + vals[0] + ":" + ex.Message);
                }
            }

            var missingfiles = filesInFolder.Except(foundfiles);
            foreach (var fif in missingfiles)
            {
                File.AppendAllText(path + "/report.txt", "File " + fif + " not found in catalog");
            }
            if (!alert && missingfiles.Count() == 0)
                progress.Report("Complete, all files verified");
            else if (alert)
                progress.Report("Complete, error found, read report.txt for info");
            else
                progress.Report("Complete, uncataloged files found, read report.txt for info");
        }

        public static void CreateChdLogic(IProgress<string> progress, string path)
        {
            foreach (var folder in Directory.EnumerateDirectories(path))
            {
                CreateChdLogic(progress, folder);
            }

            foreach (var cue in Directory.EnumerateFiles(path).Where(f => f.EndsWith(".cue") || f.EndsWith(".iso")))
            {
                progress.Report(cue);
                var results = new CHD().CreateChd(cue);
                if (results)
                {
                    if (cue.EndsWith("cue"))
                    {
                        //find referenced files that were pulled in by the cue
                        var bins = Helpers.FindBinsInCue(cue);
                        foreach (var b in bins)
                            File.Delete(path + "/" + b);
                    }
                    File.Delete(cue);
                }
            }
            //progress.Report("Complete");
        }

        public static void ExtractChdLogic(IProgress<string> progress, string path)
        {
            foreach (var folder in Directory.EnumerateDirectories(path))
            {
                ExtractChdLogic(progress, folder);
            }

            foreach (var chd in Directory.EnumerateFiles(path).Where(f => f.EndsWith(".chd")).ToList())
            {
                progress.Report(chd);
                var results = new CHD().ExtractCHD(chd);
                if (results)
                {
                    File.Delete(chd);
                }
            }
            //progress.Report("Complete");
        }

        public static void DatLogic(IProgress<string> progress, string path)
        {
            DatCreator.MakeDat(path, progress);
            progress.Report("Completed making DAT file");
        }

        public static void IdentifyLogic(IProgress<string> progress, string path, bool moveUnidentified, MemDb db)
        {
            var files = System.IO.Directory.EnumerateFiles(path).ToList();
            if (moveUnidentified)
                Directory.CreateDirectory(path + "/Unknown");

            //bool useOffsets = chkUseIDOffsets.Checked;
            string errors = "";
            Hasher h = new Hasher();
            foreach (var file in files)
            {
                try
                {
                    progress.Report(Path.GetFileName(file));
                    //Identify it first.
                    var hashes = h.HashFileAtPath(file);
                    var identifiedFiles = db.findFile(hashes);
                    if (identifiedFiles.Count > 1)
                    {
                        //TODO: duplicate entries in DAT file unhandled
                        throw new Exception("multiple entries in provided DAT file for " + file);
                    }

                    var identifiedFile = identifiedFiles.FirstOrDefault()?.name;
                    var destFileName = (!string.IsNullOrWhiteSpace(identifiedFile) ? identifiedFile : (moveUnidentified ? "/Unknown/" : "") + Path.GetFileName(file));

                    if (identifiedFile != destFileName)
                        File.Move(file, path + "/" + destFileName);

                }
                catch (Exception ex)
                {
                    errors += file + ": " + ex.Message + Environment.NewLine;
                }
            }


            if (errors != "")
            {
                progress.Report("Complete, Errors occurred: " + errors);
            }
            else
                progress.Report("Complete");
        }

        public static void IdentifyLogicMultiFile(IProgress<string> progress, string path, bool moveUnidentified, MemDb db)
        {
            //
            var files = System.IO.Directory.EnumerateFiles(path).ToList();
            if (moveUnidentified)
                Directory.CreateDirectory(path + "/Unknown");

            //bool useOffsets = chkUseIDOffsets.Checked;
            string errors = "";
            Hasher h = new Hasher();
            foreach (var file in files)
            {
                try
                {
                    progress.Report(Path.GetFileName(file));
                    //Identify it first.
                    var hashes = h.HashFileAtPath(file);
                    var identifiedFiles = db.findFile(hashes);
                    if (identifiedFiles.Count > 0)
                    {
                        //TODO: duplicate entries in DAT file unhandled
                        throw new Exception("multiple entries in provided DAT file for " + file);
                    }

                    var identifiedFile = identifiedFiles.FirstOrDefault().name; //TODO test this works as expected
                    var destFileName = (identifiedFile != "" ? identifiedFile : (moveUnidentified ? "/Unknown/" : "") + Path.GetFileName(file));

                    if (identifiedFile != destFileName)
                        File.Move(file, path + "/" + destFileName);

                }
                catch (Exception ex)
                {
                    errors += file + ": " + ex.Message + Environment.NewLine;
                }
            }


            if (errors != "")
            {
                progress.Report("Complete, Errors occurred: " + errors);
            }
            else
                progress.Report("Complete");
        }

        public static void OneGameOneRomSort(IProgress<string> progress, string path, MemDb db, List<string> regionPrefs)
        {
            var files = Directory.EnumerateFiles(path);
            Directory.CreateDirectory(path + "/1G1R/");

            foreach (var pciSet in db.parentClones)
            {
                progress.Report(pciSet.name);
                if (pciSet.Clones.Count == 1)
                {
                    if (File.Exists(path + "/" + pciSet.fileName))
                    {
                        File.Move(path + "/" + pciSet.fileName, path + "/1G1R/" + pciSet.fileName);
                    }
                }
                else
                    foreach (string pref in regionPrefs)
                    {
                        var clone = pciSet.Clones.FirstOrDefault(c => c.region == pref);
                        if (clone != null)
                        {
                            if (File.Exists(path + "/" + clone.fileName))
                            {
                                File.Move(path + "/" + clone.fileName, path + "/1G1R/" + clone.fileName);
                                break;
                            }
                        }
                    }
            }

            progress.Report("Done! Check the /1G1R folder for your set.");
        }

        public static void EverdriveSort(IProgress<string> progress, string path)
        {
            //Everdrive sort.
            //assume that the picked folder is the destination and already sorted/IDed.
            //Also, all items are in one folder.

            var fileList = System.IO.Directory.EnumerateFiles(path);
            fileList = fileList.Select(f => System.IO.Path.GetFileName(f)).ToList();

            //Create folders for each letter
            var letters = fileList.Select(f => System.IO.Path.GetFileName(f).ToUpper().Substring(0, 1)).Distinct().ToList(); //pick first letter.
            foreach (var l in letters)
            {
                progress.Report(l);
                System.IO.Directory.CreateDirectory(path + "/" + l);

                var filesToMove = fileList.Where(f => f.StartsWith(l) || f.StartsWith(l.ToLower())).ToList();
                foreach (var rf in filesToMove)
                    System.IO.File.Move(path + "/" + rf, path + "/" + l + "/" + rf);
            }

            progress.Report("Everdrive Sort completed.");
        }

        /// <summary>
        /// Finds any disk images with "disk 1" in the name, then finds any other images 
        /// with the same name and writes them to a .m3u file
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="path"></param>
        public static void CreateM3uPlaylists(IProgress<string> progress, string path)
        {
            var fileList = System.IO.Directory.EnumerateFiles(path)            
                .Where(x => x.Contains("disc 1", StringComparison.CurrentCultureIgnoreCase) 
                && (x.Contains(".chd") || x.Contains(".iso") || x.Contains(".cue")))
                .ToList();

            foreach(var file in fileList)
            {
                progress.Report(file);
                var fullTitle = System.IO.Path.GetFileName(file);
                var titlePosition = fullTitle.IndexOf("disc", StringComparison.InvariantCultureIgnoreCase);
                var partialTitle = fullTitle.Substring(0, titlePosition);

                var diskFiles = System.IO.Directory.EnumerateFiles(path)
                .Where(x => x.Contains(partialTitle, StringComparison.CurrentCultureIgnoreCase) 
                && (x.Contains(".chd") || x.Contains(".iso") || x.Contains(".cue")))
                .OrderBy(x => x)
                .Select(x => Path.GetFileName(x))
                .ToList();

                var filename = $"{partialTitle.Trim('(', '[', ' ')}.m3u";
                var outputFilePath = $"{Path.GetDirectoryName(file)}{Path.DirectorySeparatorChar}{filename}";

                File.WriteAllLines(outputFilePath, diskFiles);
            }

            progress.Report($"M3U playlists created for {fileList.Count} game(s)");
        }

        public static void ApplyAllPatches(IProgress<string> progress, string path)
        {
            var patchList = System.IO.Directory.EnumerateFiles(path).Where(x => x.ToLower().EndsWith(".ips") || x.ToLower().EndsWith(".bps") || x.ToLower().EndsWith(".ups") || x.ToLower().EndsWith(".xdelta")).ToList();
            var possibleROM = System.IO.Directory.EnumerateFiles(path).Where(x => !x.ToLower().EndsWith(".ips") && !x.ToLower().EndsWith(".bps") && !x.ToLower().EndsWith(".ups") && !x.ToLower().EndsWith(".xdelta")).ToList();
            possibleROM = possibleROM.Where(r => !r.ToLower().Contains("desktop.ini")).ToList();

            if (possibleROM.Count > 1 || possibleROM.Count == 0)
            {
                //TODO error out.
                return;
            }

            string romName = possibleROM.FirstOrDefault();
            

            foreach (var patch in patchList)
            {
                bool result = true;
                progress.Report(patch);
                var extension = Path.GetExtension(patch.ToLower());
                switch (extension)
                {
                    case ".ips":
                    case ".bps":
                        result = Patcher.PatchWithFlips(patch, romName);
                        break;
                    case ".xdelta":
                        result = Patcher.PatchWithXDelta(patch, romName);
                        break;
                    case ".ups":
                        result = Patcher.PatchWithUPS(patch, romName);
                        break;
                }

                if (!result && MoveMissedPatches)
                {
                    Directory.CreateDirectory(path + @"\Unapplied");
                    File.Move(patch, path + @"\Unapplied\" + Path.GetFileName(patch));
                }
                else
                    File.Delete(patch);
            }
            File.Delete(romName);

            progress.Report("Patching Complete.");
        }

        public static void DeletePatches(IProgress<string> progress, string path)
        {
            var patchList = System.IO.Directory.EnumerateFiles(path).Where(x => x.ToLower().EndsWith(".ips") || x.ToLower().EndsWith(".bps") || x.ToLower().EndsWith(".ups") || x.ToLower().EndsWith(".xdelta")).ToList();
            foreach (var p in patchList)
                File.Delete(p);

            progress.Report("Deleting Complete.");
        }

        public static void DeleteIfNoXDelta(IProgress<string> progress, string path)
        {
            var xdeltas =  System.IO.Directory.EnumerateFiles(path, "*.xdelta", SearchOption.AllDirectories);

            try
            {
                if (xdeltas.Count() == 0)
                    Directory.Delete(path, true);
            }
            catch { }
        }

        public static void DeleteIfNoUPS(IProgress<string> progress, string path)
        {
            var xdeltas = System.IO.Directory.EnumerateFiles(path, "*.ups", SearchOption.AllDirectories);

            try
            {
                if (xdeltas.Count() == 0)
                    Directory.Delete(path, true);
            }
            catch { }
        }

        public static void DeleteLowercase(IProgress<string> progress, string path)
        {
            var dirs = Directory.EnumerateDirectories(path);
            foreach (var dir in dirs)
            {
                var files = Directory.EnumerateFiles(dir);
                var maybeMissed = files.Where(f => f.EndsWith("IPS") || f.EndsWith("BPS") || f.EndsWith("UPS") || f.EndsWith("XDELTA")).ToList();

                try
                {
                    if (maybeMissed.Count() == 0)
                        Directory.Delete(dir, true);
                    else
                    {
                        foreach (var f in files)
                            if (!maybeMissed.Contains(f))
                                File.Delete(f);
                    }
                }
                catch { }
            }
        }

        public static void PrepareNesForMAME(IProgress<string> progress, string path)
        {
            var files = Directory.EnumerateFiles(path);
            foreach (var file in files)
            {
                //Path needs to be unzipped game.
                byte[] game = File.ReadAllBytes(file);
                byte[] output = null;

                string shortName = Path.GetFileNameWithoutExtension(file).ToLower().Substring(0, 8); //TODO: take decent guess on this programmatically.
                string endFilename = Path.GetFileNameWithoutExtension(file).ToLower() + ".prg";

                //TODO: better detection of INES 2.0 header and processing it later.
                /*
                 *     If byte 7 AND $0C = $08, and the size taking into account byte 9 does not exceed the actual size of the ROM image, then NES 2.0.
                        If byte 7 AND $0C = $04, archaic iNES.
                        If byte 7 AND $0C = $00, and bytes 12-15 are all 0, then iNES.
                        Otherwise, iNES 0.7 or archaic iNES.
                 */
                if (game.Length % 8192 == 16)
                {
                    //headered game, strip from final.
                    output = new byte[game.Length - 16];
                    game.CopyTo(output, 16);
                    File.WriteAllBytes(Path.GetDirectoryName(file) + "\\" + endFilename, output);

                    //Header processing:
                    /*
                     * Bytes 	Description
                        0-3 	Constant $4E $45 $53 $1A (ASCII "NES" followed by MS-DOS end-of-file)
                        4 	Size of PRG ROM in 16 KB units
                        5 	Size of CHR ROM in 8 KB units (value 0 means the board uses CHR RAM)
                        6 	Flags 6 – Mapper, mirroring, battery, trainer
                        7 	Flags 7 – Mapper, VS/Playchoice, NES 2.0
                        8 	Flags 8 – PRG-RAM size (rarely used extension)
                        9 	Flags 9 – TV system (rarely used extension)
                        10 	Flags 10 – TV system, PRG-RAM presence (unofficial, rarely used extension) 
                     */


                    //Mapper note:
                    //nes_pcb.hxx is what defines and connects the slot value to the actual code, if I need to work out which one to use for a given game.
                }
                else
                {
                    output = game;
                    File.Copy(file, Path.GetDirectoryName(file) + "\\" + endFilename);
                }

                //TODO Final game should be zipped up to shortname.zip.

                //Should also make a quick attempt to make the XML entry for the game.
                Hasher h = new Hasher();
                string crcHash = h.GetCRC32String(ref output);
                string sha1Hash = h.GetSHA1String(ref output);

                StringBuilder xml = new StringBuilder();
                xml.AppendLine("<software name=\"shrtname\" cloneof=\"none\" supported=\"partial\">");
                xml.AppendLine("\t<description>" + Path.GetFileNameWithoutExtension(file) + "</description>");
                xml.AppendLine("\t<year>????</year>");
                xml.AppendLine("\t<publisher>&lt;unknown&gt;</publisher>");
                xml.AppendLine("\t<info name=\"release\" value=\"xxxxxxxx\"/>");
                xml.AppendLine("\t<part name=\"cart\" interface=\"nes_cart\">"); //UXROM is marker for most homebrew games
                xml.AppendLine("\t\t<feature name=\"slot\" value=\"uxrom\"/>"); //TODO: determine from header value if possible. This IS what MAME uses to determine mapper.
                                                                                //TODO: mirroring value determined from header if possible
                xml.AppendLine("\t\t<dataarea name=\"prg\" size=\"" + game.Length + "\">");
                xml.AppendLine("\t\t\t<rom name=\"" + endFilename + "\" size=\"" + output.Length + "\" crc=\"" + crcHash + "\" sha1=\"" + sha1Hash + "\" offset=\"00000\"/>");
                xml.AppendLine("\t\t</dataarea>");
                xml.AppendLine("\t</part>");
                xml.AppendLine("</software>");

                File.WriteAllText(Path.GetDirectoryName(file) + "shortname.xml", xml.ToString());
            }
        }
    }
}
