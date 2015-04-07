using System;
using System.Xml.Serialization;

namespace AnFake.Core.NuSpec.v20
{	
	[Serializable]
	public sealed class Metadata
	{
		/// <summary>
		///     The unique identifier for the package. This is the package name that is shown when packages are listed using the
		///     Package Manager Console. These are also used when installing a package using the Install-Package command within the
		///     Package Manager Console. Package IDs may not contain any spaces or characters that are invalid in an URL. In
		///     general, they follow the same rules as .NET namespaces do. So Foo.Bar is a valid ID, Foo! and Foo Bar are not.
		/// </summary>
		/// <seealso cref="http://docs.nuget.org/docs/reference/nuspec-reference" />
		[XmlElement("id", IsNullable = false)]
		public string Id { get; set; }

		/// <summary>
		///     The version of the package, in a format like 1.2.3.
		/// </summary>
		/// <seealso cref="http://docs.nuget.org/docs/reference/nuspec-reference" />		
		[XmlIgnore]
		public Version Version { get; set; }

		/// <summary>
		///		String representation of Version property.
		/// </summary>
		/// <remarks>
		///		This property typically used for serialization/deserialization only.
		/// </remarks>
		[XmlElement("version", IsNullable = false)]
		// ReSharper disable once InconsistentNaming
		public string szVersion
		{
			get { return Version != null ? Version.ToString() : null; }
			set { Version = new Version(value); }
		}

		/// <summary>
		///     The human-friendly title of the package displayed in the Manage NuGet Packages dialog. If none is specified, the ID
		///     is used instead.
		/// </summary>
		/// <seealso cref="http://docs.nuget.org/docs/reference/nuspec-reference" />
		[XmlElement("title")]
		public string Title { get; set; }

		/// <summary>
		///     A comma-separated list of authors of the package code.
		/// </summary>
		/// <seealso cref="http://docs.nuget.org/docs/reference/nuspec-reference" />
		[XmlElement("authors", IsNullable = false)]
		public string Authors { get; set; }

		/// <summary>
		///     A comma-separated list of the package creators. This is often the same list as in authors. This is ignored when
		///     uploading the package to the NuGet.org Gallery.
		/// </summary>
		/// <seealso cref="http://docs.nuget.org/docs/reference/nuspec-reference" />
		[XmlElement("owners")]
		public string Owners { get; set; }

		/// <summary>
		///     A long description of the package. This shows up in the right pane of the Add Package Dialog as well as in the
		///     Package Manager Console when listing packages using the Get-Package command.
		/// </summary>
		/// <seealso cref="http://docs.nuget.org/docs/reference/nuspec-reference" />
		[XmlElement("description", IsNullable = false)]
		public string Description { get; set; }

		/// <summary>
		///     (v1.5) A description of the changes made in each release of the package. This field only shows up when the
		///     _Updates_ tab is selected and the package is an update to a previously installed package. It is displayed where the
		///     Description would normally be displayed.
		/// </summary>
		/// <seealso cref="http://docs.nuget.org/docs/reference/nuspec-reference" />
		[XmlElement("releaseNotes")]
		public string ReleaseNotes { get; set; }

		/// <summary>
		///     A short description of the package. If specified, this shows up in the middle pane of the Add Package Dialog. If
		///     not specified, a truncated version of the description is used instead.
		/// </summary>
		/// <seealso cref="http://docs.nuget.org/docs/reference/nuspec-reference" />
		[XmlElement("summary")]
		public string Summary { get; set; }

		/// <summary>
		///     The locale ID for the package, such as en-us.
		/// </summary>
		/// <seealso cref="http://docs.nuget.org/docs/reference/nuspec-reference" />
		[XmlElement("language")]
		public string Language { get; set; }

		/// <summary>
		///     A URL for the home page of the package.
		/// </summary>
		/// <seealso cref="http://docs.nuget.org/docs/reference/nuspec-reference" />
		[XmlElement("projectUrl", DataType = "anyURI")]
		public string ProjectUrl { get; set; }

		/// <summary>
		///     A URL for the image to use as the icon for the package in the Manage NuGet Packages dialog box. This should be a
		///     32x32-pixel .png file that has a transparent background.
		/// </summary>
		/// <seealso cref="http://docs.nuget.org/docs/reference/nuspec-reference" />
		[XmlElement("iconUrl", DataType = "anyURI")]
		public string IconUrl { get; set; }

		/// <summary>
		///     A link to the license that the package is under.
		/// </summary>
		/// <seealso cref="http://docs.nuget.org/docs/reference/nuspec-reference" />
		[XmlElement("licenseUrl", DataType="anyURI")]
		public string LicenseUrl { get; set; }

		/// <summary>
		///     (v1.5) Copyright details for the package.
		/// </summary>
		/// <seealso cref="http://docs.nuget.org/docs/reference/nuspec-reference" />
		[XmlElement("copyright")]
		public string Copyright { get; set; }

		/// <summary>
		///     A Boolean value that specifies whether the client needs to ensure that the package license (described by
		///     licenseUrl) is accepted before the package is installed.
		/// </summary>
		/// <seealso cref="http://docs.nuget.org/docs/reference/nuspec-reference" />
		[XmlElement("requireLicenseAcceptance")]
		public bool RequireLicenseAcceptance { get; set; }

		/// <summary>
		///     A space-delimited list of tags and keywords that describe the package. This information is used to help make sure
		///     users can find the package using searches in the Add Package Reference dialog box or filtering in the Package
		///     Manager Console window.
		/// </summary>
		/// <seealso cref="http://docs.nuget.org/docs/reference/nuspec-reference" />
		[XmlElement("tags")]
		public string Tags { get; set; }

		/// <summary>
		///     (v1.5) Names of assemblies under lib that are added as project references. If unspecified, all references in lib
		///     are added as project references. When specifying a reference, only specify the name and not the path inside the
		///     package.
		/// </summary>
		/// <seealso cref="http://docs.nuget.org/docs/reference/nuspec-reference" />
		[XmlArray("references")]
		[XmlArrayItem("reference")]
		public Reference[] References { get; set; }

		/// <summary>
		///     The list of dependencies for the package.
		/// </summary>
		/// <seealso cref="http://docs.nuget.org/create/nuspec-reference#specifying-dependencies" />
		[XmlArray("dependencies")]
		[XmlArrayItem("dependency")]
		public Dependency[] Dependencies { get; set; }

		// TODO: insert other
	}
}