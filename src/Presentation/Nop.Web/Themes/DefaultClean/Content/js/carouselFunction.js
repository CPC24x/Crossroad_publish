
$(window).on('load', function () {

  //owl carousels settings on site
  var owl1 = $(".home-page-product-grid .owl-carousel");
  owl1.owlCarousel({
    loop: true,
    margin: 25,
    nav: true,
    dots: false,
    responsive: {
      0: {
        items: 1,
      },
      768: {
        items: 3,
      },
      1200: {
        items: 4,
      },
    }
  });

  var owl2 = $(".book-and-authors .owl-carousel,.offerings .owl-carousel");
  owl2.owlCarousel({
    loop: true,
    margin: 25,
    nav: true,
    dots: false,
    responsive: {
      0: {
        items: 1,
        stagePadding: 20
      },
    }
  });

  var owl3 = $(".bookstores  .owl-carousel");
  owl3.owlCarousel({
    loop: true,
    margin: 25,
    nav: false,
    dots: false,
    responsive: {
      0: {
        items: 1,
      },
      768: {
        items: 3,
      },
      1200: {
        items: 4,
      },
    }
  });

  var owl4 = $(".blogsec  .owl-carousel");
  owl4.owlCarousel({
    loop: true,
    margin: 25,
    nav: false,
    dots: false,
    responsive: {
      0: {
        items: 1,
      },
      768: {
        items: 3,
      },
    }
  });

});