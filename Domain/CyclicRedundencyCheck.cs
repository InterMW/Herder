namespace Domain;

public static class CyclicRedundencyCheck
{
    public static UInt32 MODES_GENERATOR_POLY = 0xfff409U;
    // public static UInt32 crc_table[256];
    public static UInt32[] CrcTable = [ 0x3935ea, 0x1c9af5, 0xf1b77e, 0x78dbbf, 0xc397db, 0x9e31e9, 0xb0e2f0, 0x587178,
0x2c38bc, 0x161c5e, 0x0b0e2f, 0xfa7d13, 0x82c48d, 0xbe9842, 0x5f4c21, 0xd05c14,
0x682e0a, 0x341705, 0xe5f186, 0x72f8c3, 0xc68665, 0x9cb936, 0x4e5c9b, 0xd8d449,
0x939020, 0x49c810, 0x24e408, 0x127204, 0x093902, 0x049c81, 0xfdb444, 0x7eda22,
0x3f6d11, 0xe04c8c, 0x702646, 0x381323, 0xe3f395, 0x8e03ce, 0x4701e7, 0xdc7af7,
0x91c77f, 0xb719bb, 0xa476d9, 0xadc168, 0x56e0b4, 0x2b705a, 0x15b82d, 0xf52612,
0x7a9309, 0xc2b380, 0x6159c0, 0x30ace0, 0x185670, 0x0c2b38, 0x06159c, 0x030ace,
0x018567, 0xff38b7, 0x80665f, 0xbfc92b, 0xa01e91, 0xaff54c, 0x57faa6, 0x2bfd53,
0xea04ad, 0x8af852, 0x457c29, 0xdd4410, 0x6ea208, 0x375104, 0x1ba882, 0x0dd441,
0xf91024, 0x7c8812, 0x3e4409, 0xe0d800, 0x706c00, 0x383600, 0x1c1b00, 0x0e0d80,
0x0706c0, 0x038360, 0x01c1b0, 0x00e0d8, 0x00706c, 0x003836, 0x001c1b, 0xfff409,
0x000000, 0x000000, 0x000000, 0x000000, 0x000000, 0x000000, 0x000000, 0x000000,
0x000000, 0x000000, 0x000000, 0x000000, 0x000000, 0x000000, 0x000000, 0x000000,
0x000000, 0x000000, 0x000000, 0x000000, 0x000000, 0x000000, 0x000000, 0x000000];
    // Enumerable.Range(0,256)
    //     .Select(i => CheckSumLineGenerator((UInt32)i))
    //     .ToArray();

    // public static Array<UInt32> SingleBitSyndrome =>

    
    private static UInt32 CheckSumLineGenerator(UInt32 i)
    {
        UInt32 c = i << 16;
        for (int j = 0; j < 8; ++j)
        {
            if ((c & 0x800000) != 0)
            {
                c = (c << 1) ^ 0xfff409U;
            }
            else
            {
                c = c << 1;
            }
        }

        return c & 0x00FFFFFF;
    }
    public class Errorinfo
    {
        public UInt32 syndrome { get; set; }                // CRC syndrome
        public int errors { get; set; }                   // number of errors
        public sbyte[] bit = new sbyte[2];
    };
    // static void initLookupTables()
    // {
    //     sbyte i;
    //     var msg = new sbyte[112 / 8];

    //     for (i = 0; i < 112; ++i)
    //     {
    //         msg[i / 8] ^= (sbyte)(1 << (7 - (i & 7)));
    //         single_bit_syndrome[i] = modesChecksum(msg, 112);
    //         msg[i / 8] ^= (sbyte)(1 << (7 - (i & 7)));
    //     }
    // }
    // void modesChecksumInit(int fixBits)
    // {
    //     initLookupTables();

    //     // Detect out to 4 bit errors; this reduces our 2-bit coverage to about 65%.
    //     // This can take a little while - tell the user.
    //     // bitErrorTable_short = prepareErrorTable(MODES_SHORT_MSG_BITS, 2, 4, &bitErrorTableSize_short);
    //     // bitErrorTable_long = prepareErrorTable(MODES_LONG_MSG_BITS, 2, 4, &bitErrorTableSize_long);
    // }
    static int combinations(int n, int k)
    {
        int result = 1, i;

        if (k == 0 || k == n)
            return 1;

        if (k > n)
            return 0;

        for (i = 1; i <= k; ++i)
        {
            result = result * n / i;
            n = n - 1;
        }

        return result;
    }

    //     static errorinfo prepareErrorTable(int bits, int max_correct, int max_detect, out int size_out)
    //     {
    //         int maxsize, usedsize;
    //         Errorinfo base_entry = new();
    //         int i, j;


    //         if (max_correct == 0)
    //         {
    //             size_out = 0;
    //             return null;
    //         }

    //         maxsize = 0;
    //         for (i = 1; i <= max_correct; ++i)
    //         {
    //             maxsize += combinations(bits - 5, i); // space needed for all i-bit errors
    //         }

    //         var table = new Errorinfo[maxsize];
    //         base_entry.syndrome = 0;
    //         base_entry.errors = 0;
    //         for (i = 0; i < MODES_MAX_BITERRORS; ++i)
    //             base_entry.bit[i] = -1;

    //         // ignore the first 5 bits (DF type)
    //         usedsize = prepareSubtable(table, 0, maxsize, 112 - bits, 5, bits, &base_entry, 0, max_correct);


    //         qsort(table, usedsize, sizeof(struct errorinfo), syndrome_compare);


    // // Handle ambiguous cases, where there is more than one possible error pattern
    // // that produces a given syndrome (this happens with >2 bit errors).

    // for (i = 0, j = 0; i<usedsize; ++i)
    // {
    //     if (i<usedsize - 1 && table[i + 1].syndrome == table[i].syndrome)
    //     {
    //         // skip over this entry and all collisions
    //         while (i<usedsize && table[i + 1].syndrome == table[i].syndrome)
    //             ++i;

    //         // now table[i] is the last duplicate
    //         continue;
    //     }

    //     if (i != j)
    //         table[j] = table[i];
    //     ++j;
    // }

    // if (j < usedsize)
    // {
    // # ifdef CRCDEBUG
    //     fprintf(stderr, "Discarded %d collisions.\n", usedsize - j);
    // #endif
    //     usedsize = j;
    // }

    // // Flag collisions we want to detect but not correct
    // if (max_detect > max_correct)
    // {
    //     int flagged;


    //     flagged = flagCollisions(table, usedsize, 112 - bits, 5, bits, 0, 1, max_correct + 1, max_detect);

    // #endif

    //     if (flagged > 0)
    //     {
    //         for (i = 0, j = 0; i < usedsize; ++i)
    //         {
    //             if (table[i].errors != -1)
    //             {
    //                 if (i != j)
    //                     table[j] = table[i];
    //                 ++j;
    //             }
    //         }

    //         usedsize = j;
    //     }
    // }


    // *size_out = usedsize;

    // // #ifdef CRCDEBUG
    // //     {
    // //         // Check the table.
    // //         unsigned char *msg = malloc(bits/8);

    // //         for (i = 0; i < usedsize; ++i) {
    // //             int j;
    // //             struct errorinfo *ei;
    // //             uint32_t result;

    // //             memset(msg, 0, bits/8);
    // //             ei = &table[i];
    // //             for (j = 0; j < ei->errors; ++j) {
    // //                 msg[ei->bit[j] >> 3] ^= 1 << (7 - (ei->bit[j]&7));
    // //             }

    // //             result = modesChecksum(msg, bits);
    // //             if (result != ei->syndrome) {
    // //                 fprintf(stderr, "PROBLEM: entry %6d/%6d  syndrome %06x  errors %d  bits ", i, usedsize, ei->syndrome, ei->errors);
    // //                 for (j = 0; j < ei->errors; ++j)
    // //                     fprintf(stderr, "%3d ", ei->bit[j]);
    // //                 fprintf(stderr, " checksum %06x\n", result);
    // //             }
    // //         }
    // //         free(msg);

    // //         // Show the table stats
    // //         fprintf(stderr, "Syndrome table summary:\n");
    // //         for (i = 1; i <= max_correct; ++i) {
    // //             int j, count, possible;
    // //             
    // //             count = 0;
    // //             for (j = 0; j < usedsize; ++j) 
    // //                 if (table[j].errors == i)
    // //                     ++count;

    // //             possible = combinations(bits-5, i);
    // //             fprintf(stderr, "  %d entries for %d-bit errors (%d possible, %d%% coverage)\n", count, i, possible, 100 * count / possible);
    // //         }

    // //         fprintf(stderr, "  %d entries total\n", usedsize);
    // //     }
    // // #endif

    // return table;
    // }

    // Precompute syndrome tables for 56- and 112-bit messages.


    //     public static Errorinfo modesChecksumDiagnose(UInt32 syndrome, int bitlen)
    // {
    //     Errorinfo table;
    //     int tablesize;


    //     if (syndrome == 0)
    //         return new();

    //     // assert(bitlen == 56 || bitlen == 112);
    //     if (bitlen == 56) { table = bitErrorTable_short; tablesize = bitErrorTableSize_short; }
    //     else { table = bitErrorTable_long; tablesize = bitErrorTableSize_long; }

    //     if (!table)
    //         return NULL;

    //     ei.syndrome = syndrome;
    //     return bsearch(&ei, table, tablesize, sizeof(struct errorinfo), syndrome_compare);
    // }
}
