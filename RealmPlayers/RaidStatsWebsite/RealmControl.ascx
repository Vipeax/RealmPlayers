﻿<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="RealmControl.ascx.cs" Inherits="VF.RaidDamageWebsite.RealmControl" %>

<%--<form id="Form1" class="servers" runat="server">--%><div class="servers rcclcbx"><h4>Realm</h4>
    <asp:RadioButtonList ID="rblRealm" runat="server" RepeatLayout="Flow" AutoPostBack="True" Width="100px" RepeatDirection="Horizontal" CssClass="radio">
        <asp:ListItem Text="All" Value="All" />
        <asp:ListItem Text="Emerald Dream" Value="ED" />
        <asp:ListItem Text="Warsong" Value="WSG" />
        <asp:ListItem Text="Al'Akir" Value="AlA" />
        <asp:ListItem Text="Rebirth" Value="REB" />
        <asp:ListItem Text="Nostalrius" Value="NRB" />
        <asp:ListItem Text="NostalriusPVE" Value="NBE" />
        <asp:ListItem Text="Kronos" Value="KRO" />
        <asp:ListItem Text="Nefarian(DE)" Value="NEF" />
        <asp:ListItem Text="NostalGeek(FR)" Value="NG" />
        <asp:ListItem Text="Archangel(TBC)" Value="ArA" />
        <asp:ListItem Text="Valkyrie" Value="VAL" style="visibility:hidden; height:0px; line-height: 0px; overflow:hidden; display:block;"/>
        <asp:ListItem Text="VanillaGaming" Value="VG" style="visibility:hidden; height:0px; line-height: 0px; overflow:hidden; display:block;"/>
    </asp:RadioButtonList>
</div>
<%--</form>--%>