<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" 
    xmlns="http://schemas.microsoft.com/wix/2006/wi" 
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
    xmlns:wix="http://schemas.microsoft.com/wix/2006/wi">

    <xsl:output encoding="utf-8" method="xml" version="1.0" indent="yes" />

    <xsl:template match='wix:Component[contains(wix:File/@Source, "$(var.BasePath)\RadTextureViewer.exe")]'>
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
            <Shortcut Id="RadTextureViewerLnk" Name="Rad Texture Viewer" Directory="ProgramMenuFolder" WorkingDirectory="INSTALLFOLDER" Advertise="yes"/>
        </xsl:copy>
    </xsl:template>

    <xsl:template match="@*|node()">
        <xsl:copy>
            <xsl:apply-templates select="@*|node()"/>
        </xsl:copy>
    </xsl:template>

</xsl:stylesheet>