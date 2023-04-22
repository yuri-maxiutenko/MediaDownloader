<?xml version="1.0" encoding="utf-8"?>

<xsl:stylesheet
        xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
        xmlns:wix="http://schemas.microsoft.com/wix/2006/wi"
        xmlns="http://schemas.microsoft.com/wix/2006/wi"
        version="1.0"
        exclude-result-prefixes="xsl wix">

    <xsl:output method="xml" indent="yes" omit-xml-declaration="yes"/>

    <xsl:strip-space elements="*"/>

    <!--
    Find the component of the main EXE and tag it with the "ExeToRemove" key.

    Because WiX's Heat.exe only supports XSLT 1.0 and not XSLT 2.0 we cannot use `ends-with( haystack, needle )` (e.g. `ends-with( wix:File/@Source, '.exe' )`...
    ...but we can use this longer `substring` expression instead (see https://github.com/wixtoolset/issues/issues/5609 )
    -->
    <xsl:key
            name="ExeToRemove"
            match="wix:Component[ substring( wix:File/@Source, string-length( wix:File/@Source ) - 18 ) = 'MediaDownloader.exe' ]"
            use="@Id"/>

    <xsl:key
            name="PdbToRemove"
            match="wix:Component[ substring( wix:File/@Source, string-length( wix:File/@Source ) - 3 ) = '.pdb' ]"
            use="@Id"/>

    <xsl:key
            name="DbToRemove"
            match="wix:Component[ substring( wix:File/@Source, string-length( wix:File/@Source ) - 2 ) = '.db' ]"
            use="@Id"/>

    <xsl:key
            name="LogToRemove"
            match="wix:Component[ substring( wix:File/@Source, string-length( wix:File/@Source ) - 3 ) = '.log' ]"
            use="@Id"/>

    <!-- By default, copy all elements and nodes into the output... -->
    <xsl:template match="@*|node()">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
        </xsl:copy>
    </xsl:template>

    <!-- ...but if the element has the "ExeToRemove" key then don't render anything (i.e. removing it from the output) -->
    <xsl:template match="*[ self::wix:Component or self::wix:ComponentRef ][ key( 'ExeToRemove', @Id ) ]"/>
    <xsl:template match="*[ self::wix:Component or self::wix:ComponentRef ][ key( 'PdbToRemove', @Id ) ]"/>
    <xsl:template match="*[ self::wix:Component or self::wix:ComponentRef ][ key( 'DbToRemove', @Id ) ]"/>
    <xsl:template match="*[ self::wix:Component or self::wix:ComponentRef ][ key( 'LogToRemove', @Id ) ]"/>

</xsl:stylesheet>