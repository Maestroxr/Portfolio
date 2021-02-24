using System.Collections;
using System.Collections.Generic;
using Portfolio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class PlayerPrefsStorageTest : IStorageTestBase
    {

        protected override IStorageStrategy GetNewStorageInstance() => new PlayerPrefsStrategy();

    }
}
