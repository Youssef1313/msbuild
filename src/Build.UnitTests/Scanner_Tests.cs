// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Exceptions;
using Shouldly;
using Xunit;



#nullable disable

namespace Microsoft.Build.UnitTests
{
    [TestClass]
    public class ScannerTest
    {
        private MockElementLocation _elementLocation = MockElementLocation.Instance;
        /// <summary>
        /// Tests that we give a useful error position (not 0 for example)
        /// </summary>
        [Fact]
        public void ErrorPosition()
        {
            string[,] tests = {
                { "1==1.1.",                "7",    "AllowAll"},              // Position of second '.'
                { "1==0xFG",                "7",    "AllowAll"},              // Position of G
                { "1==-0xF",                "6",    "AllowAll"},              // Position of x
                { "1234=5678",              "6",    "AllowAll"},              // Position of '5'
                { " ",                      "2",    "AllowAll"},              // Position of End of Input
                { " (",                     "3",    "AllowAll"},              // Position of End of Input
                { " false or  ",            "12",   "AllowAll"},              // Position of End of Input
                { " \"foo",                 "2",    "AllowAll"},              // Position of open quote
                { " @(foo",                 "2",    "AllowAll"},              // Position of @
                { " @(",                    "2",    "AllowAll"},              // Position of @
                { " $",                     "2",    "AllowAll"},              // Position of $
                { " $(foo",                 "2",    "AllowAll"},              // Position of $
                { " $(",                    "2",    "AllowAll"},              // Position of $
                { " $",                     "2",    "AllowAll"},              // Position of $
                { " @(foo)",                "2",    "AllowProperties"},       // Position of @
                { " '@(foo)'",              "3",    "AllowProperties"},       // Position of @
                /* test escaped chars: message shows them escaped so count should include them */
                { "'%24%28x' == '%24(x''",   "21",  "AllowAll"}               // Position of extra quote
            };

            // Some errors are caught by the Parser, not merely by the Lexer/Scanner. So we have to do a full Parse,
            // rather than just calling AdvanceToScannerError(). (The error location is still supplied by the Scanner.)
            for (int i = 0; i < tests.GetLength(0); i++)
            {
                Parser parser = null;
                try
                {
                    parser = new Parser();
                    ParserOptions options = (ParserOptions)Enum.Parse(typeof(ParserOptions), tests[i, 2], true /* case-insensitive */);
                    parser.Parse(tests[i, 0], options, _elementLocation);
                }
                catch (InvalidProjectFileException ex)
                {
                    Console.WriteLine(ex.Message);
                    Assert.AreEqual(Convert.ToInt32(tests[i, 1]), parser.errorPosition);
                }
            }
        }

        /// <summary>
        /// Advance to the point of the lexer error. If the error is only caught by the parser, this isn't useful.
        /// </summary>
        /// <param name="lexer"></param>
        private void AdvanceToScannerError(Scanner lexer)
        {
            while (lexer.Advance() && !lexer.IsNext(Token.TokenType.EndOfInput))
            {
                ;
            }
        }

        /// <summary>
        /// Tests the special error for "=".
        /// </summary>
        [Fact]
        public void SingleEquals()
        {
            Scanner lexer = new Scanner("a=b", ParserOptions.AllowProperties);
            AdvanceToScannerError(lexer);
            Assert.AreEqual("IllFormedEqualsInCondition", lexer.GetErrorResource());
            Assert.AreEqual("b", lexer.UnexpectedlyFound);
        }

        /// <summary>
        /// Tests the special errors for "$(" and "$x" and similar cases
        /// </summary>
        [Fact]
        public void IllFormedProperty()
        {
            Scanner lexer = new Scanner("$(", ParserOptions.AllowProperties);
            AdvanceToScannerError(lexer);
            Assert.AreEqual("IllFormedPropertyCloseParenthesisInCondition", lexer.GetErrorResource());

            lexer = new Scanner("$x", ParserOptions.AllowProperties);
            AdvanceToScannerError(lexer);
            Assert.AreEqual("IllFormedPropertyOpenParenthesisInCondition", lexer.GetErrorResource());
        }

        /// <summary>
        /// Tests the space errors case
        /// </summary>
        [Theory]
        [InlineData("$(x )")]
        [InlineData("$( x)")]
        [InlineData("$([MSBuild]::DoSomething($(space ))")]
        [InlineData("$([MSBuild]::DoSomething($(_space ))")]
        public void SpaceProperty(string pattern)
        {
            Scanner lexer = new Scanner(pattern, ParserOptions.AllowProperties);
            AdvanceToScannerError(lexer);
            Assert.AreEqual("IllFormedPropertySpaceInCondition", lexer.GetErrorResource());
        }

        /// <summary>
        /// Tests the space not next to end so no errors case
        /// </summary>
        [Theory]
        [InlineData("$(x.StartsWith( 'y' ))")]
        [InlineData("$(x.StartsWith ('y'))")]
        [InlineData("$( x.StartsWith( $(SpacelessProperty) ) )")]
        [InlineData("$( x.StartsWith( $(_SpacelessProperty) ) )")]
        [InlineData("$(x.StartsWith('Foo', StringComparison.InvariantCultureIgnoreCase))")]
        public void SpaceInMiddleOfProperty(string pattern)
        {
            Scanner lexer = new Scanner(pattern, ParserOptions.AllowProperties);
            AdvanceToScannerError(lexer);
            lexer._errorState.ShouldBeFalse();
        }

        /// <summary>
        /// Tests the special errors for "@(" and "@x" and similar cases.
        /// </summary>
        [Fact]
        public void IllFormedItemList()
        {
            Scanner lexer = new Scanner("@(", ParserOptions.AllowAll);
            AdvanceToScannerError(lexer);
            Assert.AreEqual("IllFormedItemListCloseParenthesisInCondition", lexer.GetErrorResource());
            Assert.IsNull(lexer.UnexpectedlyFound);

            lexer = new Scanner("@x", ParserOptions.AllowAll);
            AdvanceToScannerError(lexer);
            Assert.AreEqual("IllFormedItemListOpenParenthesisInCondition", lexer.GetErrorResource());
            Assert.IsNull(lexer.UnexpectedlyFound);

            lexer = new Scanner("@(x", ParserOptions.AllowAll);
            AdvanceToScannerError(lexer);
            Assert.AreEqual("IllFormedItemListCloseParenthesisInCondition", lexer.GetErrorResource());
            Assert.IsNull(lexer.UnexpectedlyFound);

            lexer = new Scanner("@(x->'%(y)", ParserOptions.AllowAll);
            AdvanceToScannerError(lexer);
            Assert.AreEqual("IllFormedItemListQuoteInCondition", lexer.GetErrorResource());
            Assert.IsNull(lexer.UnexpectedlyFound);

            lexer = new Scanner("@(x->'%(y)', 'x", ParserOptions.AllowAll);
            AdvanceToScannerError(lexer);
            Assert.AreEqual("IllFormedItemListQuoteInCondition", lexer.GetErrorResource());
            Assert.IsNull(lexer.UnexpectedlyFound);

            lexer = new Scanner("@(x->'%(y)', 'x'", ParserOptions.AllowAll);
            AdvanceToScannerError(lexer);
            Assert.AreEqual("IllFormedItemListCloseParenthesisInCondition", lexer.GetErrorResource());
            Assert.IsNull(lexer.UnexpectedlyFound);
        }

        /// <summary>
        /// Tests the special error for unterminated quotes.
        /// Note, scanner only understands single quotes.
        /// </summary>
        [Fact]
        public void IllFormedQuotedString()
        {
            Scanner lexer = new Scanner("false or 'abc", ParserOptions.AllowAll);
            AdvanceToScannerError(lexer);
            Assert.AreEqual("IllFormedQuotedStringInCondition", lexer.GetErrorResource());
            Assert.IsNull(lexer.UnexpectedlyFound);

            lexer = new Scanner("\'", ParserOptions.AllowAll);
            AdvanceToScannerError(lexer);
            Assert.AreEqual("IllFormedQuotedStringInCondition", lexer.GetErrorResource());
            Assert.IsNull(lexer.UnexpectedlyFound);
        }

        /// <summary>
        /// </summary>
        [Fact]
        public void NumericSingleTokenTests()
        {
            Scanner lexer = new Scanner("1234", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.Numeric));
            Assert.AreEqual(0, String.Compare("1234", lexer.IsNextString()));

            lexer = new Scanner("-1234", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.Numeric));
            Assert.AreEqual(0, String.Compare("-1234", lexer.IsNextString()));

            lexer = new Scanner("+1234", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.Numeric));
            Assert.AreEqual(0, String.Compare("+1234", lexer.IsNextString()));

            lexer = new Scanner("1234.1234", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.Numeric));
            Assert.AreEqual(0, String.Compare("1234.1234", lexer.IsNextString()));

            lexer = new Scanner(".1234", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.Numeric));
            Assert.AreEqual(0, String.Compare(".1234", lexer.IsNextString()));

            lexer = new Scanner("1234.", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.Numeric));
            Assert.AreEqual(0, String.Compare("1234.", lexer.IsNextString()));
            lexer = new Scanner("0x1234", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.Numeric));
            Assert.AreEqual(0, String.Compare("0x1234", lexer.IsNextString()));
            lexer = new Scanner("0X1234abcd", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.Numeric));
            Assert.AreEqual(0, String.Compare("0X1234abcd", lexer.IsNextString()));
            lexer = new Scanner("0x1234ABCD", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.Numeric));
            Assert.AreEqual(0, String.Compare("0x1234ABCD", lexer.IsNextString()));
        }

        /// <summary>
        /// </summary>
        [Fact]
        public void PropsStringsAndBooleanSingleTokenTests()
        {
            Scanner lexer = new Scanner("$(foo)", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.Property));
            lexer = new Scanner("@(foo)", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.ItemList));
            lexer = new Scanner("abcde", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.String));
            Assert.AreEqual(0, String.Compare("abcde", lexer.IsNextString()));

            lexer = new Scanner("'abc-efg'", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.String));
            Assert.AreEqual(0, String.Compare("abc-efg", lexer.IsNextString()));

            lexer = new Scanner("and", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.And));
            Assert.AreEqual(0, String.Compare("and", lexer.IsNextString()));
            lexer = new Scanner("or", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.Or));
            Assert.AreEqual(0, String.Compare("or", lexer.IsNextString()));
            lexer = new Scanner("AnD", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.And));
            Assert.AreEqual(0, String.Compare(Token.And.String, lexer.IsNextString()));
            lexer = new Scanner("Or", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.Or));
            Assert.AreEqual(0, String.Compare(Token.Or.String, lexer.IsNextString()));
        }

        /// <summary>
        /// </summary>
        [Fact]
        public void SimpleSingleTokenTests()
        {
            Scanner lexer = new Scanner("(", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.LeftParenthesis));
            lexer = new Scanner(")", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.RightParenthesis));
            lexer = new Scanner(",", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.Comma));
            lexer = new Scanner("==", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.EqualTo));
            lexer = new Scanner("!=", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.NotEqualTo));
            lexer = new Scanner("<", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.LessThan));
            lexer = new Scanner(">", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.GreaterThan));
            lexer = new Scanner("<=", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.LessThanOrEqualTo));
            lexer = new Scanner(">=", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.GreaterThanOrEqualTo));
            lexer = new Scanner("!", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.Not));
        }


        /// <summary>
        /// </summary>
        [Fact]
        public void StringEdgeTests()
        {
            Scanner lexer = new Scanner("@(Foo, ' ')", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.ItemList));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.EndOfInput));

            lexer = new Scanner("'@(Foo, ' ')'", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.String));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.EndOfInput));

            lexer = new Scanner("'%40(( '", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.String));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.EndOfInput));

            lexer = new Scanner("'@(Complex_ItemType-123, ';')' == ''", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.String));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.EqualTo));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.String));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.EndOfInput));
        }

        /// <summary>
        /// </summary>
        [Fact]
        public void FunctionTests()
        {
            Scanner lexer = new Scanner("Foo()", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Function));
            Assert.AreEqual(0, String.Compare("Foo", lexer.IsNextString()));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.LeftParenthesis));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.RightParenthesis));

            lexer = new Scanner("Foo( 1 )", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Function));
            Assert.AreEqual(0, String.Compare("Foo", lexer.IsNextString()));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.LeftParenthesis));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Numeric));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.RightParenthesis));

            lexer = new Scanner("Foo( $(Property) )", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Function));
            Assert.AreEqual(0, String.Compare("Foo", lexer.IsNextString()));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.LeftParenthesis));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Property));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.RightParenthesis));

            lexer = new Scanner("Foo( @(ItemList) )", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Function));
            Assert.AreEqual(0, String.Compare("Foo", lexer.IsNextString()));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.LeftParenthesis));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.ItemList));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.RightParenthesis));

            lexer = new Scanner("Foo( simplestring )", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Function));
            Assert.AreEqual(0, String.Compare("Foo", lexer.IsNextString()));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.LeftParenthesis));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.String));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.RightParenthesis));

            lexer = new Scanner("Foo( 'Not a Simple String' )", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Function));
            Assert.AreEqual(0, String.Compare("Foo", lexer.IsNextString()));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.LeftParenthesis));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.String));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.RightParenthesis));

            lexer = new Scanner("Foo( 'Not a Simple String', 1234 )", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Function));
            Assert.AreEqual(0, String.Compare("Foo", lexer.IsNextString()));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.LeftParenthesis));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.String));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Comma));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Numeric));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.RightParenthesis));

            lexer = new Scanner("Foo( $(Property), 'Not a Simple String', 1234 )", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Function));
            Assert.AreEqual(0, String.Compare("Foo", lexer.IsNextString()));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.LeftParenthesis));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Property));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Comma));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.String));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Comma));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Numeric));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.RightParenthesis));

            lexer = new Scanner("Foo( @(ItemList), $(Property), simplestring, 'Not a Simple String', 1234 )", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Function));
            Assert.AreEqual(0, String.Compare("Foo", lexer.IsNextString()));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.LeftParenthesis));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.ItemList));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Comma));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Property));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Comma));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.String));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Comma));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.String));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Comma));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Numeric));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.RightParenthesis));
        }

        /// <summary>
        /// </summary>
        [Fact]
        public void ComplexTests1()
        {
            Scanner lexer = new Scanner("'String with a $(Property) inside'", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.String));
            Assert.AreEqual(0, String.Compare("String with a $(Property) inside", lexer.IsNextString()));

            lexer = new Scanner("'String with an embedded \\' in it'", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.String));
            // Assert.AreEqual(String.Compare("String with an embedded ' in it", lexer.IsNextString()), 0);

            lexer = new Scanner("'String with a $(Property) inside'", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.String));
            Assert.AreEqual(0, String.Compare("String with a $(Property) inside", lexer.IsNextString()));

            lexer = new Scanner("@(list, ' ')", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.ItemList));
            Assert.AreEqual(0, String.Compare("@(list, ' ')", lexer.IsNextString()));

            lexer = new Scanner("@(files->'%(Filename)')", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.ItemList));
            Assert.AreEqual(0, String.Compare("@(files->'%(Filename)')", lexer.IsNextString()));
        }

        /// <summary>
        /// </summary>
        [Fact]
        public void ComplexTests2()
        {
            Scanner lexer = new Scanner("1234", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());

            lexer = new Scanner("'abc-efg'==$(foo)", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.String));
            lexer.Advance();
            Assert.IsTrue(lexer.IsNext(Token.TokenType.EqualTo));
            lexer.Advance();
            Assert.IsTrue(lexer.IsNext(Token.TokenType.Property));
            lexer.Advance();
            Assert.IsTrue(lexer.IsNext(Token.TokenType.EndOfInput));

            lexer = new Scanner("$(debug)!=true", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.Property));
            lexer.Advance();
            Assert.IsTrue(lexer.IsNext(Token.TokenType.NotEqualTo));
            lexer.Advance();
            Assert.IsTrue(lexer.IsNext(Token.TokenType.String));
            lexer.Advance();
            Assert.IsTrue(lexer.IsNext(Token.TokenType.EndOfInput));

            lexer = new Scanner("$(VERSION)<5", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance());
            Assert.IsTrue(lexer.IsNext(Token.TokenType.Property));
            lexer.Advance();
            Assert.IsTrue(lexer.IsNext(Token.TokenType.LessThan));
            lexer.Advance();
            Assert.IsTrue(lexer.IsNext(Token.TokenType.Numeric));
            lexer.Advance();
            Assert.IsTrue(lexer.IsNext(Token.TokenType.EndOfInput));
        }

        /// <summary>
        /// Tests all tokens with no whitespace and whitespace.
        /// </summary>
        [Fact]
        public void WhitespaceTests()
        {
            Scanner lexer;
            Console.WriteLine("here");
            lexer = new Scanner("$(DEBUG) and $(FOO)", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Property));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.And));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Property));

            lexer = new Scanner("1234$(DEBUG)0xabcd@(foo)asdf<>'foo'<=false>=true==1234!=", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Numeric));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Property));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Numeric));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.ItemList));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.String));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.LessThan));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.GreaterThan));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.String));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.LessThanOrEqualTo));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.String));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.GreaterThanOrEqualTo));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.String));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.EqualTo));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Numeric));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.NotEqualTo));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.EndOfInput));

            lexer = new Scanner("   1234    $(DEBUG)    0xabcd  \n@(foo)    \nasdf  \n<     \n>     \n'foo'  \n<=    \nfalse     \n>=    \ntrue  \n== \n 1234    \n!=     ", ParserOptions.AllowAll);
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Numeric));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Property));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Numeric));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.ItemList));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.String));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.LessThan));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.GreaterThan));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.String));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.LessThanOrEqualTo));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.String));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.GreaterThanOrEqualTo));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.String));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.EqualTo));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.Numeric));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.NotEqualTo));
            Assert.IsTrue(lexer.Advance() && lexer.IsNext(Token.TokenType.EndOfInput));
        }

        /// <summary>
        /// Tests the parsing of item lists.
        /// </summary>
        [Fact]
        public void ItemListTests()
        {
            Scanner lexer = new Scanner("@(foo)", ParserOptions.AllowProperties);
            Assert.IsFalse(lexer.Advance());
            Assert.AreEqual(0, String.Compare(lexer.GetErrorResource(), "ItemListNotAllowedInThisConditional"));

            lexer = new Scanner("1234 '@(foo)'", ParserOptions.AllowProperties);
            Assert.IsTrue(lexer.Advance());
            Assert.IsFalse(lexer.Advance());
            Assert.AreEqual(0, String.Compare(lexer.GetErrorResource(), "ItemListNotAllowedInThisConditional"));

            lexer = new Scanner("'1234 @(foo)'", ParserOptions.AllowProperties);
            Assert.IsFalse(lexer.Advance());
            Assert.AreEqual(0, String.Compare(lexer.GetErrorResource(), "ItemListNotAllowedInThisConditional"));
        }

        /// <summary>
        /// Tests that shouldn't work.
        /// </summary>
        [Fact]
        public void NegativeTests()
        {
            Scanner lexer = new Scanner("'$(DEBUG) == true", ParserOptions.AllowAll);
            Assert.IsFalse(lexer.Advance());
        }
    }
}
