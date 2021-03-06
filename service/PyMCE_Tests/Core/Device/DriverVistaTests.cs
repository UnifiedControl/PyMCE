﻿#region License
// PyMCE - Python MCE IR Receiver Library
// Copyright 2012-2013 Dean Gardiner <gardiner91@gmail.com>
//
// Some portions of code and files are from 'IR-Server-Suite'
// Copyright 2005-2009 Team MediaPortal - http://www.team-mediaportal.com
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
//
// http://www.gnu.org/copyleft/gpl.html
#endregion

using Microsoft.VisualStudio.TestTools.UnitTesting;
using PyMCE.Core.Device;
using PyMCE.Core.Infrared;

namespace PyMCE.Tests
{
    [TestClass]
    public class DriverVistaTests
    {
        [TestMethod]
        public void DataPacketTest()
        {
            CollectionAssert.AreEqual(
                new byte[]
                    {
                        144, 1, 0, 0, 12, 254, 255, 255, 144, 1, 0, 0, 12, 254, 255, 255, 144, 1, 0, 0, 12, 254, 255,
                        255, 144, 1, 0, 0, 74, 252, 255, 255, 144, 1, 0, 0, 12, 254, 255, 255, 144, 1, 0, 0, 12, 254,
                        255, 255, 144, 1, 0, 0, 12, 254, 255, 255, 144, 1, 0, 0, 62, 254, 255, 255, 132, 3, 0, 0, 12,
                        254, 255, 255, 144, 1, 0, 0, 12, 254, 255, 255, 144, 1, 0, 0, 218, 253, 255, 255, 144, 1, 0, 0,
                        124, 252, 255, 255, 214, 6, 0, 0, 136, 250, 255, 255, 82, 3, 0, 0, 12, 254, 255, 255, 144, 1, 0,
                        0, 12, 254, 255, 255, 144, 1, 0, 0, 12, 254, 255, 255, 194, 1, 0, 0, 12, 254, 255, 255, 144, 1,
                        0, 0, 12, 254, 255, 255, 144, 1, 0, 0, 12, 254, 255, 255, 144, 1, 0, 0, 12, 254, 255, 255, 144,
                        1, 0, 0, 12, 254, 255, 255, 144, 1, 0, 0, 12, 254, 255, 255, 144, 1, 0, 0, 12, 254, 255, 255,
                        194, 1, 0, 0, 124, 252, 255, 255, 32, 3, 0, 0, 12, 254, 255, 255, 144, 1, 0, 0, 124, 252, 255,
                        255, 182, 3, 0, 0, 112, 254, 255, 255, 248, 36, 1, 0, 8, 219, 254, 255
                    },
                DriverVista.DataPacket(new IRCode(36044, new int[]
                                                             {
                                                                 388, -499, 388, -499, 388, -499,
                                                                 388, -943, 388, -499, 388, -499,
                                                                 388, -499, 388, -443, 887, -499,
                                                                 388, -499, 388, -554, 388, -887,
                                                                 1747, -1387, 860, -499, 388, -499,
                                                                 388, -499, 443, -499, 388, -499,
                                                                 388, -499, 388, -499, 388, -499,
                                                                 388, -499, 388, -499, 443, -887,
                                                                 804, -499, 388, -887, 943, -388,
                                                                 74990, -75000
                                                             })));
        }

        [TestMethod]
        public void GetHighBitTest()
        {
            Assert.AreEqual(0, DriverVista.GetHighBit(0, 2));
            Assert.AreEqual(0, DriverVista.GetHighBit(0, 1));
        }

        [TestMethod]
        public void FirstHighBitTest()
        {
            Assert.AreEqual(1, DriverVista.FirstHighBit(2));
        }

        [TestMethod]
        public void FirstLowBitTest()
        {
            Assert.AreEqual(0, DriverVista.FirstLowBit(2));
        }

        [TestMethod]
        public void GetCarrierPeriodTest()
        {
            Assert.AreEqual(28, DriverVista.GetCarrierPeriod(36044));
        }

        [TestMethod]
        public void GetTransmitModeTest()
        {
            Assert.AreEqual(DriverVista.TransmitMode.CarrierMode, DriverVista.GetTransmitMode(36682));
        }

        [TestMethod]
        public void GetTimingDataFromPacketTest()
        {
            CollectionAssert.AreEqual(
                new int[]
                    {
                        2750, -800,
                        550, -300,
                        550, -350,
                        550, -800,
                        500, -850,
                        1400, -800,
                        550, -400,
                        500, -400,
                        500, -400,
                        550, -350,
                        550, -350,
                        550, -350,
                        550

                    },
                DriverVista.GetTimingDataFromPacket(new byte[]
                                                        {
                                                            0xbe, 0x0a, 0x00, 0x00, 0xe0,
                                                            0xfc, 0xff, 0xff, 0x26, 0x02,
                                                            0x00, 0x00, 0xd4, 0xfe, 0xff,
                                                            0xff, 0x26, 0x02, 0x00, 0x00,
                                                            0xa2, 0xfe, 0xff, 0xff, 0x26,
                                                            0x02, 0x00, 0x00, 0xe0, 0xfc,
                                                            0xff, 0xff, 0xf4, 0x01, 0x00,
                                                            0x00, 0xae, 0xfc, 0xff, 0xff,
                                                            0x78, 0x05, 0x00, 0x00, 0xe0,
                                                            0xfc, 0xff, 0xff, 0x26, 0x02,
                                                            0x00, 0x00, 0x70, 0xfe, 0xff,
                                                            0xff, 0xf4, 0x01, 0x00, 0x00,
                                                            0x70, 0xfe, 0xff, 0xff, 0xf4,
                                                            0x01, 0x00, 0x00, 0x70, 0xfe,
                                                            0xff, 0xff, 0x26, 0x02, 0x00,
                                                            0x00, 0xa2, 0xfe, 0xff, 0xff,
                                                            0x26, 0x02, 0x00, 0x00, 0xa2,
                                                            0xfe, 0xff, 0xff, 0x26, 0x02,
                                                            0x00, 0x00, 0xa2, 0xfe, 0xff,
                                                            0xff, 0x26, 0x02, 0x00, 0x00
                                                        }));
        }
    }
}
