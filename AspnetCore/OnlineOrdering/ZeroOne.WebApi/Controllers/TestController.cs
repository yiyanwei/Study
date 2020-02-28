﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ZeroOne.Entity;
using ZeroOne.Extension.Global;

namespace ZeroOne.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        // GET: api/Test
        [HttpGet("Get")]
        public string Get(BoolEnum @bool)
        {
            return "bool: " + @bool;
        }

        [HttpGet("GetSomething")]
        public string GetSomething(ProInfo model)
        {
            string name = "zhangsan";
            return name;
        }
    }
}