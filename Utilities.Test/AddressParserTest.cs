using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MP.Utilities.Test
{
    /// <summary>
    /// Summary description for AddressParserTest
    /// </summary>
    [TestClass]
    public class AddressParserTest
    {
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void FloorPrefixed()
        {
            Assert.AreEqual("1", AddressParser.Parse("408-2790 Yew St., 1 floor").Floor);
            Assert.AreEqual("1", AddressParser.Parse("#408-2790 Yew St., 1st floor ").Floor);
            Assert.AreEqual("22", AddressParser.Parse("2790 Yew St., 22 floor, apt.408").Floor);
            Assert.AreEqual("22", AddressParser.Parse("2790 Yew St., 22nd floor, apt.408").Floor);
            Assert.AreEqual("105", AddressParser.Parse("2790 Yew St., 105th floor #408").Floor);
            Assert.AreEqual("main", AddressParser.Parse("2790 Yew St., main floor,#408").Floor);
            Assert.AreEqual("5", AddressParser.Parse("#23 - 7733 Turnill Street floor 5").Floor);
            Assert.AreEqual("", AddressParser.Parse("2790 Yew St., floor").Floor);
        }

        [TestMethod]
        public void FloorSuffixed()
        {
            Assert.AreEqual("1", AddressParser.Parse("2790 Yew St., floor 1,#408").Floor);
            Assert.AreEqual("22", AddressParser.Parse("2790 Yew St., floor 22, #408").Floor);
            Assert.AreEqual("105", AddressParser.Parse("2790 Yew St., floor 105 apt.408").Floor);
            Assert.AreEqual("M", AddressParser.Parse("2790 Yew St., floor M ").Floor);
            Assert.AreEqual("L2", AddressParser.Parse("2790 Yew St., floor L2").Floor);
            Assert.AreEqual("", AddressParser.Parse("2790 Yew St., floor #5").Floor);
        }

        [TestMethod]
        public void UnitNumber()
        {
            Assert.AreEqual("408", AddressParser.Parse("408-2790 Yew St.").UnitNumber);
            Assert.AreEqual("408", AddressParser.Parse("  408 - 2790 Yew St.").UnitNumber);
            Assert.AreEqual("408", AddressParser.Parse("#408-2790 Yew St.").UnitNumber);
            Assert.AreEqual("408", AddressParser.Parse("# 408  - 2790 Yew St.").UnitNumber);
            Assert.AreEqual("408", AddressParser.Parse("2790 Yew St., #408").UnitNumber);
            Assert.AreEqual("408", AddressParser.Parse("apt.408, 2790 Yew St.").UnitNumber);
            Assert.AreEqual("408", AddressParser.Parse("2790 Yew St., Apt. 408, floor 4").UnitNumber);
            Assert.AreEqual("408", AddressParser.Parse(" Unit 408, 2790 Yew St.").UnitNumber);
            Assert.AreEqual("408", AddressParser.Parse("2790 Yew St., unit 408").UnitNumber);
            Assert.AreEqual("408", AddressParser.Parse("2790 Yew St., suite 408").UnitNumber);
            Assert.AreEqual("408", AddressParser.Parse("Suite 408 - 2790 Yew St.").UnitNumber);
            Assert.AreEqual("408", AddressParser.Parse("2790 Yew St., apartment 408").UnitNumber);
            Assert.AreEqual("408", AddressParser.Parse("Apartment 408 - 2790 Yew St.").UnitNumber);
            Assert.AreEqual("408-S", AddressParser.Parse("408-S - 2790 Yew St.").UnitNumber);
            Assert.AreEqual("408-S", AddressParser.Parse("2790 Yew St., #408-S").UnitNumber);
        }

        [TestMethod]
        public void StreetNumber()
        {
            var addresses = new[]
            {
                "2790 41st Ave",
                "408-2790 41st Ave",
                "#408-2790 41st Ave",
                "2790 41st Ave, #408"
            };

            foreach (var address in addresses)
            {
                Assert.AreEqual("2790", AddressParser.Parse(address).HouseNumber, "Address: " + address);
            }
        }

        [TestMethod]
        public void StreetDesignator()
        {
            var addresses = new[]
            {
                "2790 Yew Street",
                "2790 Yew street, apt. 408",
                "2790 Yew str., apt. 408",
                "2790 Yew St.",
            };

            foreach (var address in addresses)
            {
                Assert.AreEqual("Street", AddressParser.Parse(address).StreetDesignator, "Address: " + address);
            }
        }

        [TestMethod]
        public void StreetDirectionPrefix()
        {
            var addresses = new[]
            {
                "2790 SW Marine Drive",
                "408-2790 sw marine drive",
            };

            foreach (var address in addresses)
            {
                Assert.AreEqual("SW", AddressParser.Parse(address).StreetDirPrefix, "Address: " + address);
            }
        }

        [TestMethod]
        public void StreetDirectionSuffix()
        {
            var addresses = new[]
            {
                "2790 Marine Drive SW",
                "2790 marine drive sw, apt. 408",
                "2790 marine drive sw, 408",
            };

            foreach (var address in addresses)
            {
                Assert.AreEqual("SW", AddressParser.Parse(address).StreetDirSuffix, "Address: " + address);
            }
        }

        [TestMethod]
        public void StreetDirectionPrefixAndSuffix()
        {
            var addresses = new[]
            {
                "408-2790 SW marine drive N",
                "2790 sw marine drive n, apt. 408"
            };

            foreach (var address in addresses)
            {
                var parsedAddress = AddressParser.Parse(address);
                Assert.AreEqual("SW", parsedAddress.StreetDirPrefix, "Address: " + address);
                Assert.AreEqual("N", parsedAddress.StreetDirSuffix, "Address: " + address);
            }
        }

        [TestMethod]
        public void StreetName()
        {
            var addresses = new[]
            {
                "2790 Marine Drive SW",
                "2790 Marine drive sw, apt. 408",
                "2790 Marine",
                "408-2790 Marine dr.",
                "#408-2790 sw Marine DR."
            };

            foreach (var address in addresses)
            {
                Assert.AreEqual("Marine", AddressParser.Parse(address).StreetName, "Address: " + address);
            }
        }
    }
}
