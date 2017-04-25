<?xml version="1.0" encoding="UTF-8" standalone="no"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:msxsl="urn:schemas-microsoft-com:xslt" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:my="http://schemas.microsoft.com/office/infopath/2003/myXSD/2017-04-25T02:31:05" version="1.0">
	<xsl:output encoding="UTF-8" method="xml"/>
	<xsl:template match="text() | *[namespace-uri()='http://www.w3.org/1999/xhtml']" mode="RichText">
		<xsl:copy-of select="."/>
	</xsl:template>
	<xsl:template match="/">
		<xsl:copy-of select="processing-instruction() | comment()"/>
		<xsl:choose>
			<xsl:when test="my:XSN_EXAMPLE">
				<xsl:apply-templates select="my:XSN_EXAMPLE" mode="_0"/>
			</xsl:when>
			<xsl:otherwise>
				<xsl:variable name="var">
					<xsl:element name="my:XSN_EXAMPLE"/>
				</xsl:variable>
				<xsl:apply-templates select="msxsl:node-set($var)/*" mode="_0"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	<xsl:template match="my:XSN_EXAMPLE" mode="_0">
		<xsl:copy>
			<xsl:element name="my:field1">
				<xsl:copy-of select="my:field1/text()[1]"/>
			</xsl:element>
			<xsl:element name="my:field2">
				<xsl:apply-templates select="my:field2/text() | my:field2/*[namespace-uri()='http://www.w3.org/1999/xhtml']" mode="RichText"/>
			</xsl:element>
			<xsl:element name="my:field3">
				<xsl:copy-of select="my:field3/text()[1]"/>
			</xsl:element>
			<xsl:element name="my:field4">
				<xsl:copy-of select="my:field4/text()[1]"/>
			</xsl:element>
			<xsl:element name="my:field5">
				<xsl:choose>
					<xsl:when test="my:field5/text()[1]">
						<xsl:copy-of select="my:field5/text()[1]"/>
					</xsl:when>
					<xsl:otherwise>false</xsl:otherwise>
				</xsl:choose>
			</xsl:element>
			<xsl:element name="my:field6">
				<xsl:copy-of select="my:field6/text()[1]"/>
			</xsl:element>
		</xsl:copy>
	</xsl:template>
</xsl:stylesheet>