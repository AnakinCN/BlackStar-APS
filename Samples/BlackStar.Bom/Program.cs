using BlackStar.Model;

var mainBOM = new Bom("Main Bom");
var screwsBOM = new Bom("Screws");
var nutsBOM = new Bom("Nuts");
var washersBOM = new Bom("Washers");

mainBOM.AddSubBom(screwsBOM,1);
mainBOM.AddSubBom(nutsBOM, 1);
mainBOM.AddSubBom(washersBOM, 1);

var smallScrewsBOM = new Bom("Small Screws");
var largeScrewsBOM = new Bom("Large Screws");

screwsBOM.AddSubBom(smallScrewsBOM, 1);
screwsBOM.AddSubBom(largeScrewsBOM, 1);

mainBOM.Display();