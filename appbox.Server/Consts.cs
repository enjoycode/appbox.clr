﻿using System;

namespace appbox.Server
{
    public static class Consts
    {
#if Windows
        public static readonly string LibPath = "";

#else
        public static readonly string LibPath = "lib";
#endif
    }
}
