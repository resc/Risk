﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Risk.Models.Continents
{
    public class Africa
    {
        public Country country1 { get; set; }
        public Country country2 { get; set; }
        public Country country3 { get; set; }
        public Country country4 { get; set; }
        public Country country5 { get; set; }
        public Country country6 { get; set; }

        public Africa()
        {
            country1 = new Country();
            country2 = new Country();
            country3 = new Country();
            country4 = new Country();
            country5 = new Country();
            country6 = new Country();
        }
    }
}