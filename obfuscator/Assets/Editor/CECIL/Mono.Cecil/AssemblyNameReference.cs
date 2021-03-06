//
// AssemblyNameReference.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2005 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

namespace Mono.Cecil {

	using System;
	using System.Globalization;
	using System.Security.Cryptography;
	using System.Text;

	using Mono.Cecil.Metadata;

	public class AssemblyNameReference : IMetadataScope, IReflectionStructureVisitable {

		string m_name;
		string m_culture;
		Version m_version;
		AssemblyFlags m_flags;
		byte [] m_publicKey;
		byte [] m_publicKeyToken;
		AssemblyHashAlgorithm m_hashAlgo;
		byte [] m_hash;
		MetadataToken m_token;

		public string Name {
			get { return m_name; }
			set { m_name = value; }
		}

		public string Culture {
			get { return m_culture; }
			set { m_culture = value; }
		}

		public Version Version {
			get { return m_version; }
			set { m_version = value; }
		}

		public AssemblyFlags Flags {
			get { return m_flags; }
			set { m_flags = value; }
		}

		public byte [] PublicKey {
			get { return m_publicKey; }
			set {
				m_publicKey = value;
				m_publicKeyToken = null;
			}
		}

		public byte [] PublicKeyToken {
			get {
#if !CF_1_0 && !CF_2_0
				if ((m_publicKeyToken == null || m_publicKeyToken.Length == 0) && (m_publicKey != null && m_publicKey.Length > 0)) {
					HashAlgorithm ha = null;
					switch (m_hashAlgo) {
					case AssemblyHashAlgorithm.Reserved:
						ha = MD5.Create (); break;
					default:
						// None default to SHA1
						ha = SHA1.Create (); break;
					}
					byte[] hash = ha.ComputeHash (m_publicKey);
					// we need the last 8 bytes in reverse order
					m_publicKeyToken = new byte [8];
					Array.Copy (hash, (hash.Length - 8), m_publicKeyToken, 0, 8);
					Array.Reverse (m_publicKeyToken, 0, 8);
				}
#endif
				return m_publicKeyToken;
			}
			set { m_publicKeyToken = value; }
		}

		public string FullName {
			get {
				StringBuilder sb = new StringBuilder ();
				string sep = ", ";
				sb.Append (m_name);
				if (m_version != null) {
					sb.Append (sep);
					sb.Append ("Version=");
					sb.Append (m_version.ToString ());
				}
				sb.Append (sep);
				sb.Append ("Culture=");
				sb.Append (m_culture == string.Empty ? "neutral" : m_culture);
				sb.Append (sep);
				sb.Append ("PublicKeyToken=");
				if (this.PublicKeyToken != null && m_publicKeyToken.Length > 0) {
					for (int i = 0 ; i < m_publicKeyToken.Length ; i++) {
						sb.Append (m_publicKeyToken [i].ToString ("x2"));
					}
				} else {
					sb.Append ("null");
				}
				return sb.ToString ();
			}
		}

		public static AssemblyNameReference Parse (string fullName)
		{
			if (fullName == null)
				throw new ArgumentNullException ("fullName");
			if (fullName.Length == 0)
				throw new ArgumentException ("Name can not be empty");

			AssemblyNameReference name = new AssemblyNameReference ();
			string [] tokens = fullName.Split (',');
			for (int i = 0; i < tokens.Length; i++) {
				string token = tokens [i].Trim ();

				if (i == 0) {
					name.Name = token;
					continue;
				}

				string [] parts = token.Split ('=');
				if (parts.Length != 2)
					throw new ArgumentException ("Malformed name");

				switch (parts [0]) {
				case "Version":
					name.Version = new Version (parts [1]);
					break;
				case "Culture":
					name.Culture = parts [1];
					break;
				case "PublicKeyToken":
					string pkToken = parts [1];
					if (pkToken == "null")
						break;

					name.PublicKeyToken = new byte [pkToken.Length / 2];
					for (int j = 0; j < name.PublicKeyToken.Length; j++) {
						name.PublicKeyToken [j] = Byte.Parse (pkToken.Substring (j * 2, 2), NumberStyles.HexNumber);
					}
					break;
				}
			}

			return name;
		}

		public AssemblyHashAlgorithm HashAlgorithm
		{
			get { return m_hashAlgo; }
			set { m_hashAlgo = value; }
		}

		public virtual byte [] Hash {
			get { return m_hash; }
			set { m_hash = value; }
		}

		public MetadataToken MetadataToken {
			get { return m_token; }
			set { m_token = value; }
		}

		public AssemblyNameReference () : this (string.Empty, string.Empty, new Version (0, 0, 0, 0))
		{
		}

		public AssemblyNameReference (string name, string culture, Version version)
		{
			if (name == null)
				throw new ArgumentNullException ("name");
			if (culture == null)
				throw new ArgumentNullException ("culture");
			m_name = name;
			m_culture = culture;
			m_version = version;
			m_hashAlgo = AssemblyHashAlgorithm.None;
		}

		public override string ToString ()
		{
			return this.FullName;
		}

		public virtual void Accept (IReflectionStructureVisitor visitor)
		{
			visitor.VisitAssemblyNameReference (this);
		}
	}
}
