using System.Collections;
using System.Collections.Generic;
using Portfolio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    /// <summary>
    /// Test class used as base class for all storage testing.
    /// </summary>
    abstract public class IStorageTestBase
    {
        protected IStorageStrategy Storage;
        protected string TestKey = "Test Key";
        protected int IntValue = 5;
        protected float FloatValue = 5.5f;
        protected string StringValue = "five";
        protected bool BoolValue = true;

        abstract protected IStorageStrategy GetNewStorageInstance();

        [SetUp]
        public void Setup()
        {
            Storage = GetNewStorageInstance();
        }

        [TearDown]
        public void TearDown()
        {
            Storage.DeleteAll();
        }


        [Test]
        public void SetInt_SetAndGet_SameRetrieved()
        {
            Storage.SetInt(TestKey, IntValue);
            int actual = Storage.GetInt(TestKey);
            Assert.AreEqual(IntValue, actual);
        }

        [Test]
        public void SetFloat_SetAndGet_SameRetrieved()
        {
            Storage.SetFloat(TestKey, FloatValue);
            float actual = Storage.GetFloat(TestKey);
            Assert.AreEqual(FloatValue, actual);
        }


        [Test]
        public void SetString_SetAndGet_SameRetrieved()
        {
            Storage.SetString(TestKey, StringValue);
            string actual = Storage.GetString(TestKey);
            Assert.AreEqual(StringValue, actual);
        }

        [Test]
        public void SetBool_SetAndGet_SameRetrieved()
        {
            Storage.SetBool(TestKey, BoolValue);
            bool actual = Storage.GetBool(TestKey);
            Assert.AreEqual(BoolValue, actual);
        }


        [Test]
        public void DoesKeyExist_SetAndCheck_KeyExists()
        {
            Storage.SetBool(TestKey, BoolValue);
            bool actual = Storage.DoesKeyExist(TestKey);
            Assert.AreEqual(BoolValue, actual);
        }


        [Test]
        public void DeleteByKey_SetAndDelete_KeyDeleted()
        {
            Storage.SetBool(TestKey, BoolValue);
            Storage.DeleteByKey(TestKey);
            Assert.AreEqual(false, Storage.DoesKeyExist(TestKey));
        }


        [Test]
        public void DeleteAll_SetAndDelete_AllDeleted()
        {
            Storage.SetInt("int", IntValue);
            Storage.SetFloat("float", FloatValue);
            Storage.SetString("string", StringValue);
            Storage.SetBool("bool", BoolValue);

            Storage.DeleteAll();

            Assert.AreEqual(false, Storage.DoesKeyExist("int"));
            Assert.AreEqual(false, Storage.DoesKeyExist("float"));
            Assert.AreEqual(false, Storage.DoesKeyExist("string"));
            Assert.AreEqual(false, Storage.DoesKeyExist("bool"));
        }


        [Test]
        public virtual void Persist_OneOfEachType_Success()
        {
            Storage.SetInt("int", IntValue);
            Storage.SetFloat("float", FloatValue);
            Storage.SetString("string", StringValue);
            Storage.SetBool("bool", BoolValue);
            Storage.Persist();

            IStorageStrategy newStorage = GetNewStorageInstance();
            Assert.AreEqual(IntValue, newStorage.GetInt("int"));
            Assert.AreEqual(FloatValue, newStorage.GetFloat("float"));
            Assert.AreEqual(StringValue, newStorage.GetString("string"));
            Assert.AreEqual(BoolValue, newStorage.GetBool("bool"));
        }

        
       

    }
}
