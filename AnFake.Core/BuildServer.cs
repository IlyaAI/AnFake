using System;
using System.Text;
using AnFake.Core.Integration;

namespace AnFake.Core
{
	/// <summary>
	///		Represents access point to build server integration.
	/// </summary>
	public static class BuildServer
	{
		private static readonly Lazy<IBuildServer> Instance 
			= new Lazy<IBuildServer>(Plugin.Get<IBuildServer>);

		private static IBuildServer _local;

		/// <summary>
		///		Instance of local build server.
		/// </summary>
		/// <remarks>
		///		The local instance is always available even build is ran on server. 
		///		This instance might be used for various fallback cases.
		/// </remarks>
		public static IBuildServer Local
		{
			get { return _local ?? (_local = new LocalBuildServer()); }
		}

		/// <summary>
		///		Is this build local?
		/// </summary>
		/// <remarks>
		///		Returns true is current build is ran locally (i.e. not under build server) and false otherwise.
		/// </remarks>
		public static bool IsLocal
		{
			get { return Instance.Value.IsLocal; }
		}

		/// <summary>
		///		Can this build expose artifacts?
		/// </summary>
		public static bool CanExposeArtifacts
		{
			get { return Instance.Value.CanExposeArtifacts; }
		}

		/// <summary>
		///		Exposes given file as build artifact of specified type and returns URI to access this artifact.
		///		Throws an exception if build server unable to expose artifact.
		/// </summary>
		/// <remarks>
		///		<para>
		///			Method throws if build server unable to expose artifact in any reason including: 
		///			public location isn't configured; artifact with the same name already exists; 
		///			any i/o error occured.
		///		</para>
		/// </remarks>		
		/// <param name="file">file to be exposed</param>
		/// <param name="type"><see cref="ArtifactType"/>(default Other)</param>
		/// <returns>URI of exposed artifact (not null)</returns>
		/// <example>
		/// The Team Foundation Server has an option 'Drop Folder' in build definition. This is UNC path to folder which is visible outside of build sandbox.
		/// Assume your build definition has a name 'MyBuild' and drop folder is '\\build-server\builds'.
		/// <code>
		/// let myExe = ".out/MyProduct.exe".AsFile()
		/// let uri = BuildServer.ExposeArtifact(myExe, ArtifactType.Deliverables) // file://build-server/builds/MyBuild/MyBuild_20150101.1/Deliverables/MyProduct.exe
		/// </code>
		/// </example>
		public static Uri ExposeArtifact(FileItem file, ArtifactType type = ArtifactType.Other)
		{
			if (file == null)
				throw new ArgumentException("BuildServer.ExposeArtifact(file[, type]): file must not be null");

			return Instance.Value.ExposeArtifact(file, type);
		}

		/// <summary>
		///		Exposes given folder (with all content) as build artifact of specified type and returns URI to access this artifact.
		///		Throws an exception if build server unable to expose artifact.
		/// </summary>
		/// <param name="folder">folder to be exposed</param>
		/// <param name="type"><see cref="ArtifactType"/>(default Other)</param>
		/// <returns>URI of exposed artifact (not null)</returns>
		/// <seealso cref="ExposeArtifact(AnFake.Core.FileItem,AnFake.Core.ArtifactType)"/>
		public static Uri ExposeArtifact(FolderItem folder, ArtifactType type = ArtifactType.Other)
		{
			if (folder == null)
				throw new ArgumentException("BuildServer.ExposeArtifact(folder[, type]): folder must not be null");

			return Instance.Value.ExposeArtifact(folder, type);
		}

		/// <summary>
		///		Exposes given text content as build artifact of specified type with givent name and returns URI to access this artifact.
		///		Throws an exception if build server unable to expose artifact.
		/// </summary>
		/// <param name="name">artifact name</param>
		/// <param name="content">text content to be exposed</param>
		/// <param name="encoding">content encoding</param>
		/// <param name="type"><see cref="ArtifactType"/>(default Other)</param>
		/// <returns>URI of exposed artifact (not null)</returns>
		/// <seealso cref="ExposeArtifact(AnFake.Core.FileItem,AnFake.Core.ArtifactType)"/>
		public static Uri ExposeArtifact(string name, string content, Encoding encoding, ArtifactType type = ArtifactType.Other)
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("BuildServer.ExposeArtifact(name, content, encoding[, type]): name must not be null or empty");

			if (content == null)
				throw new ArgumentException("BuildServer.ExposeArtifact(name, content, encoding[, type]): content must not be null");

			if (encoding == null)
				throw new ArgumentException("BuildServer.ExposeArtifact(name, content, encoding[, type]): encoding must not be null");

			return Instance.Value.ExposeArtifact(name, content, encoding, type);
		}

		/// <summary>
		///		Exposes files as build artifact of specified type.
		///		Throws an exception if build server unable to expose artifact.
		/// </summary>
		/// <param name="files">files to be exposed</param>
		/// <param name="type"><see cref="ArtifactType"/>(default Deliverables)</param>
		/// <returns>URI of exposed artifact (not null)</returns>
		/// <seealso cref="ExposeArtifact(AnFake.Core.FileItem,AnFake.Core.ArtifactType)"/>
		public static void ExposeArtifacts(FileSet files, ArtifactType type = ArtifactType.Deliverables)
		{
			if (files == null)
				throw new ArgumentException("BuildServer.ExposeArtifacts(files[, type]): files must not be null");

			Instance.Value.ExposeArtifacts(files, type);
		}		
	}
}