using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.VectorMath;
using MHServerEmuMini.Games.GameData;

namespace MHServerEmuMini.Games.Regions
{
    public class RegionManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly Dictionary<RegionPrototypeRef, Region> _regionDict = new();

        public Region GetRegion(RegionPrototypeRef regionProtoRef)
        {
            if (_regionDict.TryGetValue(regionProtoRef, out Region region) == false)
            {
                region = GenerateRegion(regionProtoRef);
                if (region != null) _regionDict.Add(regionProtoRef, region);
            }

            return region;
        }

        private Region GenerateRegion(RegionPrototypeRef regionProtoRef)
        {
            RegionSettings settings = new() { RegionProtoRef = (ulong)regionProtoRef };
            Region region = null;
            Area area = null;

            switch (regionProtoRef)
            {
                case RegionPrototypeRef.AvengersTowerHUBRegion:
                    settings.Min = new(-4608f);
                    settings.Max = new(4608f);
                    settings.EntrancePosition = new(1576f, -3E-06f, 242f);
                    settings.EntranceOrientation = new(-3.141641f, 0f, 0f);

                    region = new(settings);

                    area = region.AddArea(1, (ulong)AreaPrototypeRef.AvengersTowerHUBArea);
                    area.AddCell(1, HashHelper.HashPath("&Resource/Cells/DistrictCells/Avengers_Tower/AvengersTower_HUB.cell"));

                    break;

                case RegionPrototypeRef.XaviersMansionRegion:
                    settings.Min = new(-6144f, -5120f, -1043f);
                    settings.Max = new(4096f, 9216f, 1024f);
                    settings.EntrancePosition = new(-2047, 5248, -128f);

                    region = new(settings);

                    area = region.AddArea(1, (ulong)AreaPrototypeRef.XaviersMansionArea);
                    area.AddCell(1, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_A.cell"));
                    area.AddCell(2, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_AA.cell"));
                    area.AddCell(3, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_B.cell"));
                    area.AddCell(4, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_BB.cell"));
                    area.AddCell(5, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_C.cell"));
                    area.AddCell(6, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_CC.cell"));
                    area.AddCell(7, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_D.cell"));
                    area.AddCell(8, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_E.cell"));
                    area.AddCell(9, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_F.cell"));
                    area.AddCell(10, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_G.cell"));
                    area.AddCell(11, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_H.cell"));
                    area.AddCell(12, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_I.cell"));
                    area.AddCell(13, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_J.cell"));
                    area.AddCell(14, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_K.cell"));
                    area.AddCell(15, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_L.cell"));
                    area.AddCell(16, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_M.cell"));
                    area.AddCell(17, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_N.cell"));
                    area.AddCell(18, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_O.cell"));
                    area.AddCell(19, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_P.cell"));
                    area.AddCell(20, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_Q.cell"));
                    area.AddCell(21, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_R.cell"));
                    area.AddCell(22, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_S.cell"));
                    area.AddCell(23, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_T.cell"));
                    area.AddCell(24, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_U.cell"));
                    area.AddCell(25, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_V.cell"));
                    area.AddCell(26, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_W.cell"));
                    area.AddCell(27, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_X.cell"));
                    area.AddCell(28, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_Y.cell"));
                    area.AddCell(29, HashHelper.HashPath("&Resource/Cells/DistrictCells/X_Mansion/X_Mansion_Z.cell"));

                    break;

                case RegionPrototypeRef.HelicarrierRegion:
                    settings.Min = new(-4352f);
                    settings.Max = new(4352f);
                    settings.EntrancePosition = new(-405.75f, 1274.125f, 56f);

                    region = new(settings);

                    area = region.AddArea(1, (ulong)AreaPrototypeRef.HelicarrierArea);
                    area.AddCell(1, HashHelper.HashPath("&Resource/Cells/DistrictCells/Helicarrier/Helicarrier_HUB.cell"));

                    break;

                case RegionPrototypeRef.TrainingRoomSHIELDRegion:
                    settings.EntrancePosition = new(-1412f, 0f, 316f);
                    settings.Min = new(-4352f);
                    settings.Max = new(4352f);

                    region = new(settings);

                    area = region.AddArea(1, (ulong)AreaPrototypeRef.TrainingRoomSHIELDArea);
                    area.AddCell(1, HashHelper.HashPath("&Resource/Cells/DistrictCells/Training_Rooms/TrainingRoom_SHIELD_B.cell"));

                    break;

                case RegionPrototypeRef.RaftRegion:
                    settings.Min = new(-6076f, -9600f, -4096f);
                    settings.Max = new(6076f, 9600f, 4096f);
                    settings.EntrancePosition = new(97.61077f, -536.023f, 1098f);
                    settings.EntranceOrientation = new(3.927051f, 0f, 0f);

                    region = new(settings);

                    area = region.AddArea(1, (ulong)AreaPrototypeRef.RaftHelipadEntryArea);
                    area.AddCell(1, HashHelper.HashPath("&Resource/Cells/Tutorial/Tutorial_Trans/Raft_Helipad_Entry_A.cell"), Vector3.Zero);
                    area.AddCell(2, HashHelper.HashPath("&Resource/Cells/Tutorial/Tutorial_Main_A/RaftLift_Trans.cell"), new(-344f, 5888f, 432f));

                    break;

                case RegionPrototypeRef.HellsKitchen01RegionA:
                    settings.Min = new(-6912f, -10368f, -1152f);
                    settings.Max = new(6912f, 10368f, 1152f);
                    settings.EntrancePosition = new(5078.25f, -7849.25f, 70f);
                    settings.EntranceOrientation = new(-2.39f, 0f, 0f);

                    region = new(settings);

                    region.ImportLayoutFromFile("HellsKitchen01Region_28026680.json");

                    break;

                case RegionPrototypeRef.SubwayHK01Region:
                    settings.Min = new(-4480f, -8832f, -2176f);
                    settings.Max = new(4480f, 8832f, 2176f);
                    settings.EntrancePosition = new(-2173.625f, -7247.875f, 54f);
                    settings.EntranceOrientation = new(1.6f, 0f, 0f);

                    region = new(settings);

                    region.ImportLayoutFromFile("SubwayHK01Region_1359769821.json");

                    break;

                case RegionPrototypeRef.NightclubRegion:
                    settings.Min = new(-6912f, -10368f, -1152f);
                    settings.Max = new(6912f, 10368f, 1152f);
                    settings.EntrancePosition = new(-3472f, -8891.375f, -136.375f);
                    settings.EntranceOrientation = new(-1.64f, 0f, 0f);

                    region = new(settings);

                    region.ImportLayoutFromFile("NightclubRegion_623944073.json");

                    break;

                case RegionPrototypeRef.ClassifiedBovineSectorRegion:
                    settings.Min = new(-6912f, -9216f, -1152f);
                    settings.Max = new(6912f, 9216f, 1152f);
                    settings.EntrancePosition = new(3350f, 5584f, 134f);

                    region = new(settings);

                    region.ImportLayoutFromFile("ClassifiedBovineSectorRegion_1796848406.json");

                    break;

                default: return Logger.WarnReturn<Region>(null, $"GetRegion(): Unsupported region {regionProtoRef}");
            }

            return region;
        }
    }
}
