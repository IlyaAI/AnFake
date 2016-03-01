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
		///		Current build changeset id. Throws exception if VCS doesn't provide integer identifier.
		/// </summary>
		public static int CurrentChangesetId
		{
			get { return Instance.Value.CurrentChangesetId; }
		}

		/// <summary>
		///		Current build changeset hash.
		/// </summary>
		public static string CurrentChangesetHash
		{
			get { return Instance.Value.CurrentChangesetHash; }
		}

		/// <summary>
		///		Current build number. Throws exception if build server doesn't provide integer build number.
		/// </summary>
		public static int CurrentBuildCounter
		{
			get { return Instance.Value.CurrentBuildCounter; }
		}

		/// <summary>
		///		Can this build expose artifacts?
		/// </summary>
		public static bool CanExposeArtifacts
		{
			get { return Instance.Value.CanExposeArtifacts; }
		}

		/// <summary>
		///		Exposes given file as build artifact under specified target folder and returns URI to access this artifact.
		///		Throws an exception if build server unable to expose artifact.
		/// </summary>
		/// <remarks>
		///		<para>
		///			Method throws if build server unable to expose artifact in any reason including: 
		///			public location isn't configured; artifact with the same name already exists; 
		///			any i/o error occured.
		///		</para>
		///		<para>
		///			File will be exposed with its original name. 
		///			E.g. <c>ExposeArtifact("some/path/to/file.ext".AsFile(), "target/path")</c> produces artifact with path 'target/path/file.ext'.
		///		</para>
		/// </remarks>
		/// <param name="file">file to be exposed</param>
		/// <param name="targetFolder">target folder</param>
		/// <returns>URI of exposed artifact (not null)</returns>
		/// <example>
		/// The Team Foundation Server has an option 'Drop Folder' in build definition. This is UNC path to folder which is visible outside of build sandbox.
		/// Assume your build definition has a name 'MyBuild' and drop folder is '\\build-server\builds'.
		/// <code>
		/// let myExe = ".out/MyProduct.exe".AsFile()
		/// let uri = BuildServer.ExposeArtifact(myExe, "bin") // file://build-server/builds/MyBuild/MyBuild_20150101.1/bin/MyProduct.exe
		/// </code>
		/// </example>
		public static Uri ExposeArtifact(FileItem file, string targetFolder = "")
		{
			if (file == null)
				throw new ArgumentException("BuildServer.ExposeArtifact(file[, targetFolder]): file must not be null");
			if (targetFolder == null)
				throw new ArgumentException("BuildServer.ExposeArtifact(file[, targetFolder]): targetFolder must not be null");

			return Instance.Value.ExposeArtifact(file, targetFolder);
		}

		/// <summary>
		///		Exposes given folder (with all content) as build artifact under specified target folder and returns URI to access this artifact.
		///		Throws an exception if build server unable to expose artifact.
		/// </summary>
		/// <remarks>
		///		<para>
		///			All folder's content will be exposed under target folder.
		///			E.g. <c>ExposeArtifact("some/path/to/folder".AsFolder(), "target/another-folder")</c> produces artifact with path 'target/another-folder'.
		///		</para>
		/// </remarks>
		/// <param name="folder">folder to be exposed</param>
		/// <param name="targetFolder">target folder</param>
		/// <returns>URI of exposed artifact (not null)</returns>
		/// <seealso cref="ExposeArtifact(AnFake.Core.FileItem,String)"/>
		public static Uri ExposeArtifact(FolderItem folder, string targetFolder = "")
		{
			if (folder == null)
				throw new ArgumentException("BuildServer.ExposeArtifact(folder[, targetFolder]): folder must not be null");
			if (targetFolder == null)
				throw new ArgumentException("BuildServer.ExposeArtifact(folder[, targetFolder]): targetFolder must not be null");

			return Instance.Value.ExposeArtifact(folder, targetFolder);
		}

		/// <summary>
		///		Exposes given text content as build artifact under specified target folder with givent name and returns URI to access this artifact.
		///		Throws an exception if build server unable to expose artifact.
		/// </summary>
		///		<para>		
		///			E.g. <c>ExposeArtifact("file.ext", "some content", Encoding.UTF8, "target/path")</c> produces artifact with path 'target/path/file.ext'.
		///		</para>
		/// <param name="name">artifact name</param>
		/// <param name="content">text content to be exposed</param>
		/// <param name="encoding">content encoding</param>
		/// <param name="targetFolder">targetFolder</param>
		/// <returns>URI of exposed artifact (not null)</returns>
		/// <seealso cref="ExposeArtifact(AnFake.Core.FileItem,String)"/>
		public static Uri ExposeArtifact(string name, string content, Encoding encoding, string targetFolder = "")
		{
			if (String.IsNullOrEmpty(name))
				throw new ArgumentException("BuildServer.ExposeArtifact(name, content, encoding[, targetFolder]): name must not be null or empty");

			if (content == null)
				throw new ArgumentException("BuildServer.ExposeArtifact(name, content, encoding[, targetFolder]): content must not be null");

			if (encoding == null)
				throw new ArgumentException("BuildServer.ExposeArtifact(name, content, encoding[, targetFolder]): encoding must not be null");

			if (targetFolder == null)
				throw new ArgumentException("BuildServer.ExposeArtifact(name, content, encoding[, targetFolder]): targetFolder must not be null");

			return Instance.Value.ExposeArtifact(name, content, encoding, targetFolder);
		}

		/// <summary>
		///		Exposes files as build artifact under specified target folder.
		///		Throws an exception if build server unable to expose artifact.
		/// </summary>
		///		<para>
		///			Files will be exposed with those original names and relative paths under target folder.
		///			E.g. <c>ExposeArtifact("some/path/to/file1.ext".AsFileSet() + "sub/path/file2.ext".AsFileSetFrom("another/path"), "target/path")</c> 
		///			produces artifacts: 'target/path/file1.ext', 'target/path/sub/path/file2.ext'.
		///		</para>
		/// <param name="files">files to be exposed</param>
		/// <param name="targetFolder">target folder</param>
		/// <returns>URI of exposed artifact (not null)</returns>
		/// <seealso cref="ExposeArtifact(AnFake.Core.FileItem,String)"/>
		public static void ExposeArtifacts(FileSet files, string targetFolder = "")
		{
			if (files == null)
				throw new ArgumentException("BuildServer.ExposeArtifacts(files[, targetFolder]): files must not be null");
			if (targetFolder == null)
				throw new ArgumentException("BuildServer.ExposeArtifacts(files[, targetFolder]): targetFolder must not be null");

			Instance.Value.ExposeArtifacts(files, targetFolder);
		}		
	}
}