using System.Collections.Generic;
using ArenaSurvival.Meta;
using NUnit.Framework;

namespace ArenaSurvival.Tests
{
    public class ProgressionSimTests
    {
        private static List<SimStage> Stages() => new List<SimStage>
        {
            new SimStage { name = "1-1", stageNumber = 1, recommendedPower = 130, baseGold = 100, minutes = 3f },
            new SimStage { name = "1-2", stageNumber = 2, recommendedPower = 160, baseGold = 100, minutes = 3f },
            new SimStage { name = "1-3", stageNumber = 3, recommendedPower = 220, baseGold = 100, minutes = 4f },
        };

        [Test]
        public void WinChance_Boundaries()
        {
            Assert.AreEqual(0f, ProgressionSim.WinChance(70, 100), 1e-4, "dưới 75% đề xuất: thua chắc");
            Assert.AreEqual(0.15f, ProgressionSim.WinChance(75, 100), 1e-4);
            Assert.AreEqual(1f, ProgressionSim.WinChance(125, 100), 1e-4);
            Assert.AreEqual(1f, ProgressionSim.WinChance(500, 100), 1e-4);
        }

        [Test]
        public void Run_SameSeed_SameResult()
        {
            var settings = new SimSettings { seed = 5, runs = 20 };
            List<SimStageResult> a = ProgressionSim.Run(Stages(), settings);
            List<SimStageResult> b = ProgressionSim.Run(Stages(), settings);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].averagePlays, b[i].averagePlays);
                Assert.AreEqual(a[i].averageMinutes, b[i].averageMinutes);
            }
        }

        [Test]
        public void Run_StrongerStart_NeedsFewerPlays()
        {
            List<SimStageResult> weak = ProgressionSim.Run(Stages(), new SimSettings { seed = 1, runs = 50, startPower = 120 });
            List<SimStageResult> strong = ProgressionSim.Run(Stages(), new SimSettings { seed = 1, runs = 50, startPower = 300 });
            Assert.Less(strong[2].averagePlays, weak[2].averagePlays);
            Assert.AreEqual(1f, strong[0].averagePlays, 1e-4, "đủ mạnh thì 1 lượt là qua");
        }

        [Test]
        public void Run_MinutesAccumulate()
        {
            List<SimStageResult> r = ProgressionSim.Run(Stages(), new SimSettings { seed = 2, runs = 30 });
            Assert.LessOrEqual(r[0].averageMinutes, r[1].averageMinutes);
            Assert.LessOrEqual(r[1].averageMinutes, r[2].averageMinutes);
        }

        [Test]
        public void Run_ImpossibleStage_StopsAtMaxPlaysAndReportsStuck()
        {
            var stages = new List<SimStage> { new SimStage { name = "x", stageNumber = 1, recommendedPower = 100000, baseGold = 0, minutes = 1f } };
            List<SimStageResult> r = ProgressionSim.Run(stages, new SimSettings { seed = 1, runs = 3, maxPlays = 40 });
            Assert.AreEqual(1f, r[0].stuckRate, 1e-4);
        }
    }
}
