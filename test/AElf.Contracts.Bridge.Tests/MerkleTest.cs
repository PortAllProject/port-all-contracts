using AElf.Types;
using Xunit;

namespace AElf.Contracts.Bridge.Tests
{
    public class MerkleTest
    {
        [Fact]
        public void Test()
        {
            var hash1 = Hash.LoadFromHex("e7442e8a3db707f0e792b9b8b63a64575a7cf912448ff675746a58a7bf32878d");
            var hash2 = Hash.LoadFromHex("3be6ab8c6a2ea2caf702da2d89a89b8dd1f69f42d75a00f02d86b452f86660a3");
            var hash3 = Hash.LoadFromHex("6752d66fdd1fb063b0888c2d23f7a882dc4d1b12f788f3c280345538cec67626");
            var hash4 = Hash.LoadFromHex("6752d66fdd1fb063b0888c2d23f7a882dc4d1b12f788f3c280345538cec67626");
            var hash34 = HashHelper.ConcatAndCompute(hash3, hash4);
            var hash12 = HashHelper.ConcatAndCompute(hash1, hash2);
            var root = HashHelper.ConcatAndCompute(hash12, hash34);
        }
    }
}