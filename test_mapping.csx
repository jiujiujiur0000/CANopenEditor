using System;
using System.IO;
using System.Collections.Generic;
using libEDSsharp;
using LibCanOpen;
using EDSEditorGUI2.Mapper;
using EDSEditorGUI2.ViewModels;

string infile = "project.xdd";
string outfile = "test_full.xdd";

// 1. Parse XDD
EDSsharp eds = new EDSsharp();
eds.parse_xdd_1_1(infile);

// 2. Map to Protobuf
CanOpenDevice protoDev = MappingEDS.MapToProtobuffer(eds);

// 3. Map to ViewModels
Device vmDev = ProtobufferViewModelMapper.MapFromProtobuffer(protoDev);

// 4. Map back to Protobuf
CanOpenDevice protoDev2 = ProtobufferViewModelMapper.MapToProtobuffer(vmDev);

// 5. Map back to EDS
EDSsharp eds2 = MappingEDS.MapFromProtobuffer(protoDev2);

// 6. Save to XDD
EDSsharp.export_xdd_1_1(outfile, new List<EDSsharp> { eds2 });

Console.WriteLine("Done.");
