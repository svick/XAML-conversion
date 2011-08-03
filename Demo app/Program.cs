using System;
using XamlConversion;

namespace Demo_app
{
    class Program
    {
        static void Main()
        {
            string xaml = @"
<ListView Name=""listView"" Margin=""0,0,0,164"">
    <ListView.View>
        <GridView>
            <GridView.Columns>
                <GridViewColumn Header=""Devise"" DisplayMemberBinding=""{Binding Path=devise}"" Width=""80"" />
                <GridViewColumn Header=""Libelle"" DisplayMemberBinding=""{Binding Path=label}"" Width=""120"" />
                <GridViewColumn Header=""Unite"" DisplayMemberBinding=""{Binding Path=unite}"" Width=""80"" />
                <GridViewColumn Header=""Achat"" DisplayMemberBinding=""{Binding Path=achatBanque}"" Width=""80"" />
                <GridViewColumn Header=""Vente"" DisplayMemberBinding=""{Binding Path=venteBanque}"" Width=""80"" />
            </GridView.Columns>
        </GridView>
    </ListView.View>
</ListView>";
            Console.WriteLine(new XamlConvertor().ConvertToString(xaml));
        }
    }
}