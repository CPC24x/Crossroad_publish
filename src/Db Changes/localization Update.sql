UPDATE localestringresource SET ResourceValue="{0}" WHERE ResourceName ="ShoppingCart.HeaderQuantity";
UPDATE localestringresource SET ResourceValue="items" WHERE ResourceName ="ShoppingCart";
UPDATE localestringresource SET ResourceValue="Home" WHERE ResourceName ="HomePage";


UPDATE localestringresource SET ResourceValue="We would love for you to receive our newsletter and update emails. Please subscribe here." WHERE ResourceName ="Newsletter.Title";
INSERT INTO `crossroad`.`localestringresource` (`ResourceName`, `ResourceValue`, `LanguageId`) VALUES ('Newsletter.Unsubscribe', 'Unsubscribe Your Email', '1');

UPDATE localestringresource SET ResourceValue="Keep In Touch With Us" WHERE ResourceName ="footer.followus";

