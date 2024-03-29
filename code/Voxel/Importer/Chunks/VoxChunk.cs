﻿namespace Deathcard.Importer;

public class VoxChunk : IChunk
{
	public string ChunkID { get; set; }
	public int Bytes { get; set; }

	public IChunk[] Children { get; set; }

	public VoxChunk() { }
	public VoxChunk( byte[] data ) { }

	public T GetChild<T>() where T : VoxChunk
		=> (T)Children.FirstOrDefault( c => typeof( T ) == c.GetType() );

	public IEnumerable<T> GetChildren<T>() where T : VoxChunk
		=> (IEnumerable<T>)Children.Where( c => typeof( T ) == c.GetType() );
}
