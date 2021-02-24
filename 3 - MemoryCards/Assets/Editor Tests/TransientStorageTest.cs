using System;
using System.Collections;
using System.Collections.Generic;
using Portfolio;
using NUnit.Framework;
using UnityEngine;
namespace Tests
{
    public class TransientStorageTest : IStorageTestBase
    {
        protected override IStorageStrategy GetNewStorageInstance() => new TransientStrategy();

        [Test]
        public override void Persist_OneOfEachType_Success()
        {
            Assert.Throws<NotImplementedException>(Storage.Persist);
        }
    }
}