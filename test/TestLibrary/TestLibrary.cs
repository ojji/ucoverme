using System;
using System.IO;

namespace TestLibrary
{
    public class TestLibrary
    {
        public int Add(int left, int right)
        {
            int number = 10;
            DoSomething();
            DoSomethingElse();
            return left + right;
        }

        public void Unary()
        {
            object o = null;
            var derp = (o == null ? new object() : 2) ?? new object();
        }

        public string GetBackgroundColor(string foreground_color)
        {
            string background_color = "black";
            switch (foreground_color)
            {
                case "Red":
                    background_color = "Yellow";
                    break;
                case "White":
                    background_color = "Green";
                    break;
                case "Blue":
                    background_color = "Gray";
                    break;
            }
            return background_color;
        }

        public int IfTest(int number)
        {
            if (number == 0) return 1;

            return IfTest(0);
        }


        public int TernaryIfTest(int left, int right)
        {
            int result = left <
                         right ? 0
                : 1;
            return result;
        }

        public static void Conditionals(bool first, bool second)
        {
            if (first)
            {
                Console.WriteLine("Segment 1");
            }
            if (second)
            {
                Console.WriteLine("Segment 2");

                if (first)
                {
                    Console.WriteLine("Segment 3");
                }
            }
            else
            {
                Console.WriteLine("Segment 4");
            }
        }

        public bool IfThenTest(int number)
        {
            bool result;
            if (number == 0)
            {
                result = false;
            }
            else
            {
                result = true;
            }

            return result;
        }

        public void HasSwitch(int input)
        {
            int value;
            switch (input)
            {
                case 0:
                    value = 0;
                    break;
                case 1:
                    value = 1;
                    break;
                case 2:
                    value = 2;
                    break;
            }

            goto derp;
            derp: return;
        }

        public bool NCoverTest()
        {
            int i = 10;
            {
                DoSomething(); DoSomethingElse();
            }
            return (i == GetMagicNumber()) ? ItWas10() : ItWasnt();
        }

        private bool ItWasnt()
        {
            return false;
        }

        private bool ItWas10()
        {
            return true;
        }

        private int GetMagicNumber()
        {
            return 10;
        }

        private void DoSomethingElse()
        {
        }

        private void DoSomething()
        {
        }

        public void HasSwitchWithDefault(int input)
        {
            int value;
            switch (input)
            {
                case 0:
                    value = 0;
                    break;
                case 1:
                    value = 1;
                    break;
                case 2:
                    value = 2;
                    break;
                default:
                    value = 3;
                    break;
            }
        }

        public void GeneratedFinallyBlock()
        {
            using (var stream = new MemoryStream())
            {
                ;
            }
        }

        public string HasSimpleUsingStatement()
        {
            string value;
            try
            {

            }
            finally
            {
                using (var stream = new MemoryStream())
                {
                    var x = stream.Length;
                    value = x > 1000 ? "yes" : "no";
                }
            }
            return value;
        }

        public string HasSimpleUsingStatementInsideFinally()
        {
            string value;
            try
            {

            }
            catch (ArgumentException)
            {
            }
            finally
            {
                using (var stream = new MemoryStream())
                {
                    var x = stream.Length;
                    value = x > 1000 ? "yes" : "no";
                }
            }
            return value;
        }

        public string TryCatchFinallyTest()
        {
            string value;
            try
            {
                value = "try";
                Console.WriteLine("Try!");
            }
            catch (ArgumentNullException ex)
            {
                value = "catch 1";
                Console.WriteLine("Catch 1!");
            }
            catch (Exception e)
            {
                value = "catch 2";
                Console.WriteLine("Catch 2!");
            }
            finally
            {
                value = "finally";
                Console.WriteLine("Finally!");
            }

            return value;
        }
    }
}