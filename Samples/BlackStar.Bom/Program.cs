using BlackStar.Model;

var mainBOM = new Bom("Main Bom");
var screwsBOM = new Bom("Screws");
var nutsBOM = new Bom("Nuts");
var washersBOM = new Bom("Washers");

mainBOM.AddSubBOM(screwsBOM,1);
mainBOM.AddSubBOM(nutsBOM, 1);
mainBOM.AddSubBOM(washersBOM, 1);

var smallScrewsBOM = new Bom("Small Screws");
var largeScrewsBOM = new Bom("Large Screws");

screwsBOM.AddSubBOM(smallScrewsBOM, 1);
screwsBOM.AddSubBOM(largeScrewsBOM, 1);

mainBOM.Display();