using System;
using System.Collections.Generic;
using System.Text;

namespace AnFake.Core.Integration
{
	/// <summary>
	///		Represents extension point for build server integration.
	/// </summary>
	public interface IBuildServer
	{
		/// <summary>
		///		Is this local build?
		/// </summary>
		bool IsLocal { get; }

		/// <summary>
		///		Can this build expose artifacts?
		/// </summary>
		bool CanExposeArtifacts { get; }		

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
		Uri ExposeArtifact(FileItem file, string targetFolder);

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
		Uri ExposeArtifact(FolderItem folder, string targetFolder);

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
		Uri ExposeArtifact(string name, string content, Encoding encoding, string targetFolder);

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
		void ExposeArtifacts(IEnumerable<FileItem> files, string targetFolder);
	}	
}