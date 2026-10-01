using ArenaSurvival.Economy;
using ArenaSurvival.Meta;
using NUnit.Framework;

namespace ArenaSurvival.Tests
{
    public class EconomyServiceTests
    {
        private PlayerProfile _profile;
        private EconomyService _economy;
        private int _dirtyCount;

        [SetUp]
        public void SetUp()
        {
            _profile = new PlayerProfile();
            _dirtyCount = 0;
            _economy = new EconomyService(_profile, () => _dirtyCount++);
        }

        [Test]
        public void NewProfile_HasZeroBalance()
        {
            Assert.AreEqual(0, _economy.GetBalance(CurrencyIds.Gold));
            Assert.AreEqual(0, _economy.GetBalance(CurrencyIds.Gem));
        }

        [Test]
        public void Add_IncreasesBalanceAndMarksDirty()
        {
            Assert.IsTrue(_economy.Add(CurrencyIds.Gold, 150));
            Assert.AreEqual(150, _economy.GetBalance(CurrencyIds.Gold));
            Assert.AreEqual(1, _dirtyCount);
        }

        [TestCase(0)]
        [TestCase(-10)]
        public void Add_ZeroOrNegative_Rejected(long amount)
        {
            Assert.IsFalse(_economy.Add(CurrencyIds.Gold, amount));
            Assert.AreEqual(0, _economy.GetBalance(CurrencyIds.Gold));
            Assert.AreEqual(0, _dirtyCount);
        }

        [Test]
        public void TrySpend_Enough_Deducts()
        {
            _economy.Add(CurrencyIds.Gold, 100);
            Assert.IsTrue(_economy.TrySpend(CurrencyIds.Gold, 60));
            Assert.AreEqual(40, _economy.GetBalance(CurrencyIds.Gold));
        }

        [Test]
        public void TrySpend_Exact_GoesToZero()
        {
            _economy.Add(CurrencyIds.Gold, 100);
            Assert.IsTrue(_economy.TrySpend(CurrencyIds.Gold, 100));
            Assert.AreEqual(0, _economy.GetBalance(CurrencyIds.Gold));
        }

        [Test]
        public void TrySpend_NotEnough_FailsAndChangesNothing()
        {
            _economy.Add(CurrencyIds.Gold, 50);
            int dirtyBefore = _dirtyCount;

            Assert.IsFalse(_economy.TrySpend(CurrencyIds.Gold, 51));
            Assert.AreEqual(50, _economy.GetBalance(CurrencyIds.Gold));
            Assert.AreEqual(dirtyBefore, _dirtyCount);
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void TrySpend_ZeroOrNegative_Rejected(long amount)
        {
            // Trừ số âm = cộng tiền lén: phải chặn.
            _economy.Add(CurrencyIds.Gold, 10);
            Assert.IsFalse(_economy.TrySpend(CurrencyIds.Gold, amount));
            Assert.AreEqual(10, _economy.GetBalance(CurrencyIds.Gold));
        }

        [Test]
        public void Currencies_AreIndependent()
        {
            _economy.Add(CurrencyIds.Gold, 10);
            Assert.IsFalse(_economy.TrySpend(CurrencyIds.Gem, 1));
            Assert.AreEqual(10, _economy.GetBalance(CurrencyIds.Gold));
        }

        [Test]
        public void OnCurrencyChanged_ReportsIdAndNewBalance()
        {
            string id = null; long balance = -1;
            _economy.OnCurrencyChanged += (i, b) => { id = i; balance = b; };

            _economy.Add(CurrencyIds.Gold, 30);
            Assert.AreEqual(CurrencyIds.Gold, id);
            Assert.AreEqual(30, balance);

            _economy.TrySpend(CurrencyIds.Gold, 10);
            Assert.AreEqual(20, balance);
        }

        [Test]
        public void Add_NearMaxValue_DoesNotOverflowToNegative()
        {
            _economy.Add(CurrencyIds.Gold, long.MaxValue - 5);
            _economy.Add(CurrencyIds.Gold, 100);
            Assert.AreEqual(long.MaxValue, _economy.GetBalance(CurrencyIds.Gold));
        }
    }
}
