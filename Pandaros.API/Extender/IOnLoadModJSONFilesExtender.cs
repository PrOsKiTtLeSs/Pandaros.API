﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pandaros.API.Extender
{
    public interface IOnLoadModJSONFilesExtender : IPandarosExtention
    {
        void OnLoadModJSONFiles(List<ModLoader.LoadModJSONFileContext> contexts);
    }
}