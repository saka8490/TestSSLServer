using System;
using System.Collections.Generic;
using System.IO;

/*
 * A class for testing SSLv2 support. It just takes a single connection
 * attempt.
 */

class SSL2 {

	/*
	 * A constant SSLv2 CLIENT-HELLO message. Only one connection
	 * is needed for SSLv2, since the server response will contain
	 * _all_ the cipher suites that the server is willing to
	 * support.
	 *
	 * Note: when (mis)interpreted as a SSLv3+ record, this message
	 * apparently encodes some data of (invalid) 0x80 type, using
	 * protocol version TLS 44.1, and record length of 2 bytes.
	 * Thus, the receiving part will quickly conclude that it will
	 * not support that, instead of stalling for more data from the
	 * client.
	 */
	internal static byte[] SSL2_CLIENT_HELLO = {
		0x80, 0x2E,              // header (record length)
		0x01,                    // message type (CLIENT HELLO)
		0x00, 0x02,              // version (0x0002)
		0x00, 0x15,              // cipher specs list length
		0x00, 0x00,              // session ID length
		0x00, 0x10,              // challenge length
		0x01, 0x00, 0x80,        // SSL_CK_RC4_128_WITH_MD5
		0x02, 0x00, 0x80,        // SSL_CK_RC4_128_EXPORT40_WITH_MD5
		0x03, 0x00, 0x80,        // SSL_CK_RC2_128_CBC_WITH_MD5
		0x04, 0x00, 0x80,        // SSL_CK_RC2_128_CBC_EXPORT40_WITH_MD5
		0x05, 0x00, 0x80,        // SSL_CK_IDEA_128_CBC_WITH_MD5
		0x06, 0x00, 0x40,        // SSL_CK_DES_64_CBC_WITH_MD5
		0x07, 0x00, 0xC0,        // SSL_CK_DES_192_EDE3_CBC_WITH_MD5
		0x54, 0x54, 0x54, 0x54,  // challenge data (16 bytes)
		0x54, 0x54, 0x54, 0x54,
		0x54, 0x54, 0x54, 0x54,
		0x54, 0x54, 0x54, 0x54
	};

	/*
	 * Get the cipher suites from the ServerHello. The list presumably
	 * follows server preferences, but in SSL 2.0 the client chooses
	 * the cipher suite, not the server.
	 */
	internal int[] CipherSuites {
		get {
			return cipherSuites;
		}
	}

	/*
	 * Get the server certificate. In SSL 2.0, there is only a
	 * single certificate, not a chain.
	 *
	 * There is only a single defined certificate type in the
	 * SSL 2.0 specification draft, for an X.509 certificate, so
	 * one can assume that the certificate data should be
	 * interpreted as an X.509 certificate.
	 */
	internal byte[] Certificate {
		get {
			return certificate;
		}
	}

	int[] cipherSuites;
	byte[] certificate;

	SSL2(Stream ss)
	{
		// Record length
		byte[] buf = new byte[2];
		M.ReadFully(ss, buf);
		int len = M.Dec16be(buf, 0);
		if ((len & 0x8000) == 0) {
			throw new IOException("not a SSLv2 record");
		}
		len &= 0x7FFF;
		if (len < 11) {
			throw new IOException("not a SSLv2 server hello");
		}
		buf = new byte[11];
		M.ReadFully(ss, buf);
		if (buf[0] != 0x04) {
			throw new IOException("not a SSLv2 server hello");
		}
		int certLen = M.Dec16be(buf, 5);
		int csLen = M.Dec16be(buf, 7);
		int connIdLen = M.Dec16be(buf, 9);
		if (len != 11 + certLen + csLen + connIdLen) {
			throw new IOException("not a SSLv2 server hello");
		}
		if (csLen == 0 || csLen % 3 != 0) {
			throw new IOException("not a SSLv2 server hello");
		}
		certificate = new byte[certLen];
		M.ReadFully(ss, certificate);
		byte[] cs = new byte[csLen];
		M.ReadFully(ss, cs);
		byte[] connId = new byte[connIdLen];
		M.ReadFully(ss, connId);
		cipherSuites = new int[csLen / 3];
		for (int i = 0, j = 0; i < csLen; i += 3, j ++) {
			cipherSuites[j] = M.Dec24be(cs, i);
		}
	}

	/*
	 * Send a SSL 2.0 ClientHello on the provided stream, and
	 * parse the answer. If a ServerHello was received, the
	 * returned object will contain the relevant data. If not,
	 * then this method returns null.
	 */
	internal static SSL2 TestServer(Stream ss)
	{
		try {
			ss.Write(SSL2_CLIENT_HELLO,
				0, SSL2_CLIENT_HELLO.Length);
			return new SSL2(ss);
		} catch (Exception) {
			return null;
		}
	}
}
