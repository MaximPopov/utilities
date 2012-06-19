using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MP.Utilities.Test
{
    /// <summary>
    /// Test suite for NameParser class.
    /// </summary>
    [TestClass]
    public class NameParserTest
    {
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
        public void Title()
        {
            Assert.AreEqual("Mr.", NameParser.Parse("Mr. Pink").Title);
            Assert.AreEqual("Miss", NameParser.Parse("miss Sunshine").Title);
            Assert.AreEqual("Dr.", NameParser.Parse("DR MacCoy").Title);
            Assert.AreEqual("Rev.", NameParser.Parse("rev").Title);
        }

        [TestMethod]
        public void Suffix()
        {
            Assert.AreEqual("I", NameParser.Parse("John Doe I").Suffix);
            Assert.AreEqual("IV", NameParser.Parse("John Doe iv").Suffix);
            Assert.AreEqual("Sr.", NameParser.Parse("George Bush SR.").Suffix);
            Assert.AreEqual("Jr.", NameParser.Parse("jr").Suffix);
        }

        [TestMethod]
        public void FirstOrLastName()
        {
            // No title or suffix: must be a first name.
            var name = NameParser.Parse("John");
            Assert.AreEqual("John", name.FirstName);
            Assert.AreEqual("", name.MiddleName);
            Assert.AreEqual("", name.LastName);

            // Title or suffix: must be a last name.
            name = NameParser.Parse("Mr. Spock");
            Assert.AreEqual("", name.FirstName);
            Assert.AreEqual("", name.MiddleName);
            Assert.AreEqual("Spock", name.LastName);
        }

        [TestMethod]
        public void LastNamePrefix()
        {
            Assert.AreEqual("van der Graaf", NameParser.Parse("van der Graaf").LastName);
            Assert.AreEqual("del Rio", NameParser.Parse("Ms. Rebecca del Rio").LastName);
            Assert.AreEqual("von Braun", NameParser.Parse("Matthias von Braun").LastName);
            Assert.AreEqual("de los Llanos", NameParser.Parse("Sra. de los Llanos").LastName);
        }

        [TestMethod]
        public void MiddleName()
        {
            var name = NameParser.Parse("Philip Howard Lovecraft");
            Assert.AreEqual("Philip", name.FirstName);
            Assert.AreEqual("Howard", name.MiddleName);
            Assert.AreEqual("Lovecraft", name.LastName);
        }

        [TestMethod]
        public void SpecialCharacters()
        {
            var name = NameParser.Parse("Esmeralda Villa-Lobos");
            Assert.AreEqual("Esmeralda", name.FirstName);
            Assert.AreEqual("", name.MiddleName);
            Assert.AreEqual("Villa-Lobos", name.LastName);

            name = NameParser.Parse("Peter O'Toole");
            Assert.AreEqual("Peter", name.FirstName);
            Assert.AreEqual("", name.MiddleName);
            Assert.AreEqual("O'Toole", name.LastName);
        }

        [TestMethod]
        public void ReversedName()
        {
            var name = NameParser.Parse("Cousteau,  Jacques-Yves, ");
            Assert.AreEqual("Jacques-Yves", name.FirstName);
            Assert.AreEqual("", name.MiddleName);
            Assert.AreEqual("Cousteau", name.LastName);
        }

        [TestMethod]
        public void FullNameWithNoise()
        {
            var name = NameParser.Parse("## ( MR Edward J. Hawkins  jR.  ) ");
            Assert.AreEqual("Mr.", name.Title);
            Assert.AreEqual("Edward", name.FirstName);
            Assert.AreEqual("J", name.MiddleName);
            Assert.AreEqual("Hawkins", name.LastName);
            Assert.AreEqual("Jr.", name.Suffix);
        }

        [TestMethod]
        public void EmptyName()
        {
            var name = NameParser.Parse("");

            Assert.AreEqual("", name.Title);
            Assert.AreEqual("", name.FirstName);
            Assert.AreEqual("", name.MiddleName);
            Assert.AreEqual("", name.LastName);
            Assert.AreEqual("", name.Suffix);
        }

        [TestMethod]
        public void NoName()
        {
            var name = NameParser.Parse(" #@ (@?!:*: ");

            Assert.AreEqual("", name.Title);
            Assert.AreEqual("", name.FirstName);
            Assert.AreEqual("", name.MiddleName);
            Assert.AreEqual("", name.LastName);
            Assert.AreEqual("", name.Suffix);
        }
    }
}
