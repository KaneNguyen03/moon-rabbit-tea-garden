using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace VuonTra.Core.Tests
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class MonetizationOnAttribute : Attribute, ITestAction
    {
        readonly bool _on;
        readonly Stack<bool> _before = new Stack<bool>();

        public MonetizationOnAttribute(bool on = true) => _on = on;

        public ActionTargets Targets => ActionTargets.Suite | ActionTargets.Test;

        public void BeforeTest(ITest test)
        {
            _before.Push(Features.Monetization);
            Features.Monetization = _on;
        }

        public void AfterTest(ITest test)
        {
            if (_before.Count > 0) Features.Monetization = _before.Pop();
        }
    }
}
