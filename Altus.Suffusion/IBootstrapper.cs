﻿using Altus.Suffusion.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altus.Suffusion
{
    public interface IBootstrapper
    {
        IResolveTypes Initialize();
        string InstanceName { get; }
        ulong InstanceId { get; }
        byte[] InstanceCryptoKey { get; }
    }
}
