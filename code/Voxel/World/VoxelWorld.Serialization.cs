namespace Deathcard;

partial class VoxelWorld
{
	// TODO @ceitine: add compression
	public byte[] Serialize()
	{
		using var stream = new MemoryStream();
		using var writer = new BinaryWriter( stream );

		// Header
		writer.Write( Chunks.Count );

		// Chunks
		foreach ( var (position, chunk) in Chunks )
		{
			writer.Write( position.x );
			writer.Write( position.y );
			writer.Write( position.z );

			// Voxels
			var i = stream.Position;
			var count = (ushort)0;

			stream.Seek( i + sizeof( ushort ), SeekOrigin.Begin );
			for ( byte x = 0; x < Chunk.DEFAULT_WIDTH; x++ )
			for ( byte y = 0; y < Chunk.DEFAULT_DEPTH; y++ )
			for ( byte z = 0; z < Chunk.DEFAULT_HEIGHT; z++ )
			{
				var voxel = chunk.GetVoxel( x, y, z );
				if ( voxel == null )
					continue;
				
				var type = IVoxel.GetBlockType( voxel.GetType() );
				writer.Write( x );
				writer.Write( y );
				writer.Write( z );
				writer.Write( (byte)type );
				voxel.Write( writer );

				count++;
			}

			var j = stream.Position;
			stream.Seek( i, SeekOrigin.Begin );
			writer.Write( count );
			stream.Seek( j, SeekOrigin.Begin );
		}

		// Convert stream to array.
		return stream.ToArray();
	}

	public void Deserialize( byte[] data )
	{
		using var stream = new MemoryStream( data );
		using var reader = new BinaryReader( stream );

		// Header
		var amount = reader.ReadInt32();

		// Chunks
		Chunks = new();
		for ( int i = 0; i < amount; i++ )
		{
			var chunkX = reader.ReadInt16();
			var chunkY = reader.ReadInt16();
			var chunkZ = reader.ReadInt16();

			var chunk = new Chunk( chunkX, chunkY, chunkZ, Chunks );
			Chunks.Add( new( chunkX, chunkY, chunkZ ), chunk );

			// Voxels
			var count = reader.ReadUInt16();
			for ( int j = 0; j < count; j++ )
			{
				var x = reader.ReadByte();
				var y = reader.ReadByte();
				var z = reader.ReadByte();
				var voxel = IVoxel.TryRead( reader );
				if ( voxel == null )
				{
					Log.Error( $"VoxelWorld - Tried to load invalid voxel." );
					return;
				}

				chunk.SetVoxel( x, y, z, voxel );
			}
		}
	}

	[ConCmd]
	public static void TestLoad()
	{
		IVoxel.ResetTypeLibrary();
		var world = All.FirstOrDefault();
		var buffer = world.Serialize().Compress();
		world.Deserialize( buffer.Decompress() );
	}
}
