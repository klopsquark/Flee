// Types the expression script files in TestScripts expect: the expression owner with its
// fields (bytea, sbytea, int16a, ...) and the helper types its members use.
//
// Ported to C# from TestTypes.vb of the original VB.NET Flee test project
// (Copyright (c) 2007 Eugene Ciloci, GNU LGPL 2.1 or later), as preserved in
// https://github.com/george-playstudiosasia/PlayStudios.Flee (Tests/TestTypes.vb).
// The port keeps every member, value and access modifier of the original, because the
// script files depend on them; only the language changed.

#nullable disable
#pragma warning disable 0649, 0169, 0414 // fields are read by expressions through reflection

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Flee.PublicTypes;

namespace Flee.Test.ScriptTests
{
    // System.AppDomainInitializer does not exist on .NET Core. The scripts only need a
    // delegate type of that name and signature, so the test project declares its own.
    public delegate void AppDomainInitializer(string[] args);

    public struct Mouse
    {
        public string S;
        public int I;
        public DateTime DT;
        public static DateTime SharedDT;

        public Mouse(string s, int i)
        {
            S = s;
            I = i;
            DT = new DateTime(2007, 1, 1);
        }

        public int GetI()
        {
            return I;
        }

        public int GetYear(DateTime dt)
        {
            return dt.Year;
        }

        public DateTime this[int i] => DT;

        public DateTime this[int i, string s] => DT;

        public int this[string s, int i] => i * 2;
    }

    public class Monitor
    {
        public int I;
        public string S;
        public DateTime DT;
        public static string SharedString = "string";

        public Monitor()
        {
            I = 900;
            S = "monitor";
            DT = new DateTime(2007, 1, 1);
        }

        public int GetI()
        {
            return I;
        }

        public static implicit operator double(Monitor value)
        {
            return 1.0;
        }

        public DateTime this[int i] => DT;

        public DateTime this[double d, string s] => DT;

        public int this[string s, params int[] args] => -100;
    }

    public struct Keyboard
    {
        public Mouse StructA;
        public Monitor ClassA;
    }

    public class ExpressionOwner
    {
        private double DoubleA;
        private float SingleA;
        private int Int32A;
        private string StringA;
        private bool BoolA;
        private Type TypeA;
        private byte ByteA;
        private byte ByteB;
        private sbyte SByteA;
        private short Int16A;
        private ushort UInt16A;
        private int[] IntArr = { 100, 200, 300 };
        private string[] StringArr = { "a", "b", "c" };
        private double[] DoubleArr = { 1.1, 2.2, 3.3 };
        private bool[] BoolArr = { true, false, true };
        private char[] CharArr = { '.' };
        private DateTime[] DateTimeArr = { new DateTime(2007, 7, 1) };
        private IList List;
        private System.Collections.Specialized.StringDictionary StringDict;
        private Guid GuidA;
        private DateTime DateTimeA;
        private ICloneable ICloneableA;
        private ICollection ICollectionA;
        private Version VersionA;
        private TestStruct StructA;
        private IComparable IComparableA;
        private object ObjectIntA;
        private object ObjectStringA;
        private ValueType ValueTypeStructA;
        private Exception ExceptionA;
        private Exception ExceptionNull;
        private IComparable IComparableString;
        private IComparable IComparableNull;
        private ICloneable ICloneableArray;
        private Delegate DelegateANull;
        private Array ArrayA;
        private AppDomainInitializer DelegateA;
        private System.Text.ASCIIEncoding[] AsciiEncodingArr = { };
        private System.Text.Encoding EncodingA;
        private Keyboard KeyboardA;
        private decimal DecimalA;
        private decimal DecimalB;
        private object NullField;
        private object InstanceA;
        private ArrayList InstanceB;
        private Hashtable Dict;
        private Dictionary<string, int> GenericDict;
        private DataRow Row;

        public ExpressionOwner()
        {
            InstanceB = new ArrayList();
            InstanceA = InstanceB;
            NullField = null;
            DecimalA = 100;
            DecimalB = 0.25m;
            KeyboardA = new Keyboard();
            KeyboardA.StructA = new Mouse("mouse", 123);
            KeyboardA.ClassA = new Monitor();
            EncodingA = System.Text.Encoding.ASCII;
            DelegateA = DoAction;
            ICloneableArray = new string[] { };
            ArrayA = new string[] { };
            DelegateANull = null;
            IComparableNull = null;
            IComparableString = "string";
            ExceptionA = new ArgumentException();
            ExceptionNull = null;
            ValueTypeStructA = new TestStruct();
            ObjectStringA = "string";
            ObjectIntA = 100;
            IComparableA = 100.25;
            StructA = new TestStruct();
            VersionA = new Version(1, 1, 1, 1);
            ICloneableA = "abc";
            GuidA = Guid.NewGuid();
            List = new ArrayList();
            List.Add("a");
            List.Add(100);
            StringDict = new System.Collections.Specialized.StringDictionary();
            StringDict.Add("key", "value");
            DoubleA = 100.25;
            SingleA = 100.25F;
            Int32A = 100000;
            StringA = "string";
            BoolA = true;
            TypeA = typeof(string);
            ByteA = 50;
            ByteB = 2;
            SByteA = -10;
            Int16A = -10;
            UInt16A = 100;
            DateTimeA = new DateTime(2007, 7, 1);
            GenericDict = new Dictionary<string, int>();
            GenericDict.Add("a", 100);
            GenericDict.Add("b", 100);

            Dict = new Hashtable();
            Dict.Add(100, null);
            Dict.Add("abc", null);

            DataTable dt = new DataTable();
            dt.Columns.Add("ColumnA", typeof(int));
            dt.Rows.Add(100);
            Row = dt.Rows[0];
        }

        private void DoAction(string[] args)
        {
        }

        public void DoStuff()
        {
        }

        public int DoubleIt(int i)
        {
            return i * 2;
        }

        public string FuncString()
        {
            return "abc";
        }

        public static int SharedFuncInt()
        {
            return 100;
        }

        private string PrivateFuncString()
        {
            return "abc";
        }

        // Public despite its name, as in the original.
        public static int PrivateSharedFuncInt()
        {
            return 100;
        }

        public DateTime GetDateTime()
        {
            return DateTimeA;
        }

        public int ThrowException()
        {
            throw new InvalidOperationException("Should not be thrown!");
        }

        public ArrayList Func1(ArrayList al)
        {
            return al;
        }

        public string ReturnNullString()
        {
            return null;
        }

        public int Sum(int i)
        {
            return 1;
        }

        public int Sum(int i1, int i2)
        {
            return 2;
        }

        public int Sum(int i1, double i2)
        {
            return 3;
        }

        public int Sum(params int[] args)
        {
            return 4;
        }

        public int Sum2(int i1, double i2)
        {
            return 3;
        }

        public int Sum2(params int[] args)
        {
            return 4;
        }

        public int Sum4(params int[] args)
        {
            int sum = 0;
            foreach (int i in args)
            {
                sum += i;
            }
            return sum;
        }

        public int ParamArray1(string a, params object[] args)
        {
            return 1;
        }

        public int ParamArray2(params DateTime[] args)
        {
            return 1;
        }

        public int ParamArray3(params DateTime[] args)
        {
            return 1;
        }

        public int ParamArray3()
        {
            return 2;
        }

        public int ParamArray4(params int[] args)
        {
            return 1;
        }

        public int ParamArray4(params object[] args)
        {
            return 2;
        }

        public double DoubleAProp => DoubleA;

        private int Int32AProp => Int32A;

        internal static string SharedPropA => "sharedprop";
    }

    internal class AccessTestExpressionOwner
    {
        private int PrivateField1;
        [ExpressionOwnerMemberAccess(true)]
        private int PrivateField2;
        [ExpressionOwnerMemberAccess(false)]
        private int PrivateField3;
        public int PublicField1;
    }

    internal class OverloadTestExpressionOwner
    {
        public System.IO.MemoryStream A;
        public object B;

        public int ValueType1(int arg) { return 1; }
        public int ValueType1(float arg) { return 2; }
        public int ValueType1(double arg) { return 3; }
        public int ValueType1(decimal arg) { return 4; }

        public int ValueType2(float arg) { return 1; }
        public int ValueType2(double arg) { return 2; }

        public int ValueType3(double arg) { return 1; }
        public int ValueType3(decimal arg) { return 2; }

        public int ReferenceType1(object arg) { return 1; }
        public int ReferenceType1(string arg) { return 2; }

        public int ReferenceType2(object arg) { return 1; }
        public int ReferenceType2(System.IO.MemoryStream arg) { return 2; }

        public int ReferenceType3(object arg) { return 1; }
        public int ReferenceType3(IComparable arg) { return 2; }

        public int ReferenceType4(IFormattable arg) { return 1; }
        public int ReferenceType4(IComparable arg) { return 2; }

        public int Value_ReferenceType1(int arg) { return 1; }
        public int Value_ReferenceType1(object arg) { return 2; }

        public int Value_ReferenceType2(ValueType arg) { return 1; }
        public int Value_ReferenceType2(object arg) { return 2; }

        public int Value_ReferenceType3(IComparable arg) { return 1; }
        public int Value_ReferenceType3(object arg) { return 2; }

        public int Value_ReferenceType4(IComparable arg) { return 1; }
        public int Value_ReferenceType4(IFormattable arg) { return 2; }

        public int Access1(object arg) { return 1; }
        [ExpressionOwnerMemberAccess(false)]
        public int Access1(string arg) { return 2; }

        [ExpressionOwnerMemberAccess(false)]
        public int Access2(object arg) { return 1; }
        [ExpressionOwnerMemberAccess(false)]
        public int Access2(string arg) { return 2; }

        public int Multiple1(float arg1, double arg2) { return 1; }
        public int Multiple1(int arg1, double arg2) { return 2; }
    }

    internal class TestImport
    {
        public static int DoStuff()
        {
            return 100;
        }
    }

    internal struct TestStruct : IComparable
    {
        private int MyA;

        public TestStruct(int a)
        {
            MyA = a;
        }

        public int DoStuff()
        {
            return 100;
        }

        public int CompareTo(object obj)
        {
            return 0;
        }
    }

    /// <summary>
    /// Provides a custom type descriptor and delegates everything else to its parent.
    /// Registered for int and string to test virtual properties.
    /// </summary>
    internal sealed class UselessTypeDescriptionProvider : TypeDescriptionProvider
    {
        internal UselessTypeDescriptionProvider(TypeDescriptionProvider parent) : base(parent)
        {
        }

        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
        {
            return new UselessCustomTypeDescriptor(base.GetTypeDescriptor(objectType, instance));
        }
    }

    /// <summary>
    /// Adds one property, "Name", to the original set of properties.
    /// </summary>
    internal sealed class UselessCustomTypeDescriptor : CustomTypeDescriptor
    {
        internal UselessCustomTypeDescriptor(ICustomTypeDescriptor parent) : base(parent)
        {
        }

        public override PropertyDescriptorCollection GetProperties()
        {
            PropertyDescriptorCollection originalProperties = base.GetProperties();
            List<PropertyDescriptor> newProperties = new List<PropertyDescriptor>();
            foreach (PropertyDescriptor pd in originalProperties)
            {
                newProperties.Add(pd);
            }

            newProperties.Add(new CustomPropertyDescriptor());

            return new PropertyDescriptorCollection(newProperties.ToArray(), true);
        }
    }

    internal class CustomPropertyDescriptor : PropertyDescriptor
    {
        public CustomPropertyDescriptor() : base("Name", null)
        {
        }

        public override bool CanResetValue(object component) => false;

        public override Type ComponentType => typeof(int);

        public override object GetValue(object component) => "prop!";

        public override bool IsReadOnly => false;

        public override Type PropertyType => typeof(string);

        public override void ResetValue(object component)
        {
        }

        public override void SetValue(object component, object value)
        {
        }

        public override bool ShouldSerializeValue(object component) => false;
    }

    public class NestedA
    {
        public class NestedPublicB
        {
            public static int DoStuff()
            {
                return 100;
            }
        }

        internal class NestedInternalB
        {
            public static int DoStuff()
            {
                return 100;
            }
        }
    }
}
