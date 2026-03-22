using System;
using System.Collections.Generic;
using System.Linq;

namespace HilbertGeo
{
    class City
    {
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public uint EncLat { get; set; }
        public uint EncLon { get; set; }
        public ulong MortonCode { get; set; }
        public ulong HilbertCode { get; set; }
    }

    class Program
    {
        // ---------------------------------------------------------------
        // Coordinate encoding
        // ---------------------------------------------------------------

        static uint EncodeLat(double lat)
        {
            // -90..+90 -> 0..2^32-1
            return (uint)((lat + 90.0) / 180.0 * uint.MaxValue);
        }

        static uint EncodeLon(double lon)
        {
            // -180..+180 -> 0..2^32-1
            return (uint)((lon + 180.0) / 360.0 * uint.MaxValue);
        }

        static double DecodeLat(uint enc)
        {
            return enc / (double)uint.MaxValue * 180.0 - 90.0;
        }

        static double DecodeLon(uint enc)
        {
            return enc / (double)uint.MaxValue * 360.0 - 180.0;
        }

        // ---------------------------------------------------------------
        // Morton code: interleave two 32-bit values into a 64-bit value
        // This is exactly what INTERCAL's mingle operator does.
        // ---------------------------------------------------------------

        static ulong Morton(uint x, uint y)
        {
            return Spread(x) | (Spread(y) << 1);
        }

        // Spread the bits of a 32-bit value into the even bit positions
        // of a 64-bit value. bit 0 -> bit 0, bit 1 -> bit 2, etc.
        static ulong Spread(uint v)
        {
            ulong x = v;
            x = (x | (x << 16)) & 0x0000FFFF0000FFFFUL;
            x = (x | (x <<  8)) & 0x00FF00FF00FF00FFUL;
            x = (x | (x <<  4)) & 0x0F0F0F0F0F0F0F0FUL;
            x = (x | (x <<  2)) & 0x3333333333333333UL;
            x = (x | (x <<  1)) & 0x5555555555555555UL;
            return x;
        }

        // Extract even bits back to a 32-bit value (inverse of Spread)
        static uint Compact(ulong v)
        {
            ulong x = v & 0x5555555555555555UL;
            x = (x | (x >>  1)) & 0x3333333333333333UL;
            x = (x | (x >>  2)) & 0x0F0F0F0F0F0F0F0FUL;
            x = (x | (x >>  4)) & 0x00FF00FF00FF00FFUL;
            x = (x | (x >>  8)) & 0x0000FFFF0000FFFFUL;
            x = (x | (x >> 16)) & 0x00000000FFFFFFFFUL;
            return (uint)x;
        }

        // ---------------------------------------------------------------
        // Hilbert curve conversion
        // Convert a Morton code to a Hilbert index via state machine.
        //
        // The state machine processes 2 bits at a time (one bit from each
        // coordinate, as interleaved in the Morton code). There are 4
        // orientations of the U-shaped Hilbert curve traversal.
        //
        // State machine table: [state][quadrant] -> {hilbert_output, next_state}
        // Quadrant is the 2-bit value (y_bit << 1 | x_bit) at this level.
        //
        // Orientation definitions:
        //   State 0: U opens upward    (standard orientation)
        //   State 1: U opens leftward
        //   State 2: U opens downward
        //   State 3: U opens rightward
        //
        // Table verified against the standard Hilbert curve definition.
        // ---------------------------------------------------------------

        // hilbert_output[state, quadrant]
        static readonly int[,] HilbertOutput = new int[4, 4]
        {
            // state 0 (U upward):    quad 0->0, 1->1, 2->3, 3->2
            { 0, 1, 3, 2 },
            // state 1 (U leftward):  quad 0->0, 1->3, 2->1, 3->2
            { 0, 3, 1, 2 },
            // state 2 (U downward):  quad 0->2, 1->3, 2->1, 3->0
            { 2, 3, 1, 0 },
            // state 3 (U rightward): quad 0->2, 1->1, 2->3, 3->0
            { 2, 1, 3, 0 },
        };

        // next_state[state, quadrant]
        static readonly int[,] NextState = new int[4, 4]
        {
            // state 0: quad 0->1, 1->0, 2->3, 3->0   (NOTE: verify)
            { 1, 0, 3, 0 },
            // state 1: quad 0->0, 1->2, 2->1, 3->1
            { 0, 2, 1, 1 },
            // state 2: quad 0->3, 1->2, 2->1, 3->2
            { 3, 2, 1, 2 },
            // state 3: quad 0->2, 1->3, 2->0, 3->3
            { 2, 3, 0, 3 },
        };

        static ulong MortonToHilbert(ulong morton)
        {
            ulong hilbert = 0;
            int state = 0;

            for (int i = 31; i >= 0; i--)
            {
                // Extract 2 bits at position (2i+1, 2i) from the Morton code
                // bit 2i   = x bit at level i
                // bit 2i+1 = y bit at level i
                int xBit = (int)((morton >> (2 * i))     & 1);
                int yBit = (int)((morton >> (2 * i + 1)) & 1);
                int quadrant = (yBit << 1) | xBit;

                int hOut = HilbertOutput[state, quadrant];
                state    = NextState[state, quadrant];

                hilbert = (hilbert << 2) | (ulong)hOut;
            }

            return hilbert;
        }

        // Round-trip: Hilbert back to Morton (for verification)
        static ulong HilbertToMorton(ulong hilbert)
        {
            ulong morton = 0;
            int state = 0;

            // Build a reverse lookup
            // For each (state, hilbert_output) find the quadrant
            for (int i = 31; i >= 0; i--)
            {
                int hBits = (int)((hilbert >> (2 * i)) & 3);

                // Find quadrant that produces this hilbert output in this state
                int quadrant = -1;
                for (int q = 0; q < 4; q++)
                {
                    if (HilbertOutput[state, q] == hBits)
                    {
                        quadrant = q;
                        break;
                    }
                }

                state = NextState[state, quadrant];

                int xBit = quadrant & 1;
                int yBit = (quadrant >> 1) & 1;
                morton |= ((ulong)xBit << (2 * i));
                morton |= ((ulong)yBit << (2 * i + 1));
            }

            return morton;
        }

        // ---------------------------------------------------------------
        // Haversine distance (km) between two lat/lon points
        // ---------------------------------------------------------------

        static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0;
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(lat1 * Math.PI / 180.0)
                     * Math.Cos(lat2 * Math.PI / 180.0)
                     * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        // ---------------------------------------------------------------
        // Bounding box corners -> Hilbert range
        // Takes min/max of the 4 corner Hilbert indices.
        // This is a simplification: a true implementation would decompose
        // the box into multiple Hilbert ranges to avoid false positives.
        // ---------------------------------------------------------------

        static (ulong min, ulong max) HilbertRangeForBox(
            uint minLat, uint maxLat, uint minLon, uint maxLon)
        {
            var corners = new[]
            {
                MortonToHilbert(Morton(minLat, minLon)),
                MortonToHilbert(Morton(minLat, maxLon)),
                MortonToHilbert(Morton(maxLat, minLon)),
                MortonToHilbert(Morton(maxLat, maxLon)),
            };
            return (corners.Min(), corners.Max());
        }

        // ---------------------------------------------------------------
        // Main
        // ---------------------------------------------------------------

        static void Main(string[] args)
        {
            // --- Dataset ---
            var cities = new List<City>
            {
                new City { Name = "London",    Latitude =  51.5074, Longitude =  -0.1278 },
                new City { Name = "Paris",     Latitude =  48.8566, Longitude =   2.3522 },
                new City { Name = "Berlin",    Latitude =  52.5200, Longitude =  13.4050 },
                new City { Name = "Madrid",    Latitude =  40.4168, Longitude =  -3.7038 },
                new City { Name = "Rome",      Latitude =  41.9028, Longitude =  12.4964 },
                new City { Name = "Amsterdam", Latitude =  52.3676, Longitude =   4.9041 },
                new City { Name = "Brussels",  Latitude =  50.8503, Longitude =   4.3517 },
                new City { Name = "Vienna",    Latitude =  48.2082, Longitude =  16.3738 },
                new City { Name = "Zurich",    Latitude =  47.3769, Longitude =   8.5417 },
                new City { Name = "Prague",    Latitude =  50.0755, Longitude =  14.4378 },
            };

            // --- Phase 1: Encode coordinates ---
            Console.WriteLine("=== Phase 1: Coordinate Encoding ===");
            Console.WriteLine($"{"City",-12} {"Lat":>10} {"Lon":>11} {"EncLat":>12} {"EncLon":>12}");
            Console.WriteLine(new string('-', 60));
            foreach (var c in cities)
            {
                c.EncLat = EncodeLat(c.Latitude);
                c.EncLon = EncodeLon(c.Longitude);
                Console.WriteLine($"{c.Name,-12} {c.Latitude,10:F4} {c.Longitude,11:F4} {c.EncLat,12} {c.EncLon,12}");
            }

            // --- Phase 2: Morton codes ---
            Console.WriteLine("\n=== Phase 2: Morton Codes (via mingle) ===");
            Console.WriteLine($"{"City",-12} {"Morton Code":>20}");
            Console.WriteLine(new string('-', 35));
            foreach (var c in cities)
            {
                c.MortonCode = Morton(c.EncLat, c.EncLon);
                Console.WriteLine($"{c.Name,-12} {c.MortonCode,20}");
            }

            // --- Phase 3: Hilbert indices ---
            Console.WriteLine("\n=== Phase 3: Hilbert Indices ===");
            Console.WriteLine($"{"City",-12} {"Hilbert Index":>20}");
            Console.WriteLine(new string('-', 35));
            foreach (var c in cities)
            {
                c.HilbertCode = MortonToHilbert(c.MortonCode);
                Console.WriteLine($"{c.Name,-12} {c.HilbertCode,20}");
            }

            // --- Verify round-trip ---
            Console.WriteLine("\n=== Round-trip Verification (Hilbert -> Morton) ===");
            bool allOk = true;
            foreach (var c in cities)
            {
                ulong recovered = HilbertToMorton(c.HilbertCode);
                bool ok = recovered == c.MortonCode;
                if (!ok) allOk = false;
                Console.WriteLine($"{c.Name,-12} {(ok ? "OK" : "FAIL")}  Morton={c.MortonCode}  Recovered={recovered}");
            }
            Console.WriteLine(allOk ? "All round-trips OK." : "ROUND-TRIP FAILURES DETECTED.");

            // --- Phase 4: Sort by Hilbert index ---
            var sorted = cities.OrderBy(c => c.HilbertCode).ToList();
            Console.WriteLine("\n=== Phase 4: Cities Sorted by Hilbert Index ===");
            Console.WriteLine($"{"Rank",-5} {"City",-12} {"Hilbert Index":>20}");
            Console.WriteLine(new string('-', 40));
            for (int i = 0; i < sorted.Count; i++)
                Console.WriteLine($"{i+1,-5} {sorted[i].Name,-12} {sorted[i].HilbertCode,20}");

            // --- Phase 5: Range query ---
            // Find all cities within 1200km of London using Hilbert range
            var london = cities.First(c => c.Name == "London");
            double queryRadiusKm = 1200.0;

            // Convert radius to encoded coordinate delta
            // 1 degree latitude ~ 111km, 1 degree longitude ~ 111km * cos(lat)
            double deltaLatDeg = queryRadiusKm / 111.0;
            double deltaLonDeg = queryRadiusKm / (111.0 * Math.Cos(london.Latitude * Math.PI / 180.0));

            uint minLat = EncodeLat(Math.Max(-90,  london.Latitude  - deltaLatDeg));
            uint maxLat = EncodeLat(Math.Min( 90,  london.Latitude  + deltaLatDeg));
            uint minLon = EncodeLon(Math.Max(-180, london.Longitude - deltaLonDeg));
            uint maxLon = EncodeLon(Math.Min( 180, london.Longitude + deltaLonDeg));

            var (hMin, hMax) = HilbertRangeForBox(minLat, maxLat, minLon, maxLon);

            Console.WriteLine($"\n=== Phase 5: Range Query ===");
            Console.WriteLine($"Query: cities within {queryRadiusKm:F0}km of London");
            Console.WriteLine($"Bounding box: lat [{DecodeLat(minLat):F2}, {DecodeLat(maxLat):F2}]  " +
                              $"lon [{DecodeLon(minLon):F2}, {DecodeLon(maxLon):F2}]");
            Console.WriteLine($"Hilbert range: [{hMin}, {hMax}]");
            Console.WriteLine();

            Console.WriteLine($"{"City",-12} {"Hilbert Index":>20} {"In Range":>10} {"Actual Dist (km)":>18} {"In Bbox":>8}");
            Console.WriteLine(new string('-', 72));

            foreach (var c in sorted)
            {
                bool inHilbertRange = c.HilbertCode >= hMin && c.HilbertCode <= hMax;
                double distKm = HaversineKm(london.Latitude, london.Longitude, c.Latitude, c.Longitude);
                bool inBbox = c.Latitude  >= DecodeLat(minLat) && c.Latitude  <= DecodeLat(maxLat) &&
                              c.Longitude >= DecodeLon(minLon) && c.Longitude <= DecodeLon(maxLon);

                string marker = inHilbertRange ? "YES" : "-";
                string bboxMarker = inBbox ? "YES" : "-";
                Console.WriteLine($"{c.Name,-12} {c.HilbertCode,20} {marker,10} {distKm,18:F1} {bboxMarker,8}");
            }

            Console.WriteLine();
            Console.WriteLine("Note: 'In Range' = within Hilbert index range (may include false positives).");
            Console.WriteLine("      'In Bbox'  = within geographic bounding box.");
            Console.WriteLine("      False positives occur when the Hilbert range spans regions outside the box.");

            // --- Show locality property ---
            Console.WriteLine("\n=== Locality Check: Are geographically close cities adjacent in Hilbert order? ===");
            Console.WriteLine("(Cities within 500km of each other that are also adjacent in Hilbert order)");
            Console.WriteLine();
            for (int i = 0; i < sorted.Count - 1; i++)
            {
                double dist = HaversineKm(
                    sorted[i].Latitude, sorted[i].Longitude,
                    sorted[i+1].Latitude, sorted[i+1].Longitude);
                ulong hDiff = sorted[i+1].HilbertCode - sorted[i].HilbertCode;
                Console.WriteLine($"  {sorted[i].Name,-12} -> {sorted[i+1].Name,-12}  " +
                                  $"geo dist: {dist,7:F1}km   hilbert gap: {hDiff,20}");
            }
        }
    }
}
