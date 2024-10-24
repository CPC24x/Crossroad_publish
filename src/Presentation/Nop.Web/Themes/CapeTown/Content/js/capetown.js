$(document).ready(function () {

  $('.sub-category-grid .item-grid').owlCarousel({
    nav: true,
    loop: false,
    dots: true,
    margin: 30,
    autoplay: true,
    autoplayTimeout: 3000,
    autoplayHoverPause: true,
    lazyLoad: true,
    responsive: {
      0: {
        items: 1
      },
      576: {
        items: 1
      },
      768: {
        items: 2
      },
      1200: {
        items: 3
      }
    }
    });


  if ($(window).width() < 1001) {
    $('.header-selectors-wrapper').appendTo('.mm-selector-wrapper');
    $('.user-dropdown .dropdown-menu').appendTo('.mm-header-links');
    $('.mm-header-links .dropdown-menu').addClass('user-menu');
    $('.mm-header-links .dropdown-menu').removeClass('dropdown-menu');
    $('.mm-navbar.mobile').appendTo('body');
    $('.sublist-toggle').each(function () {
      var menuLink = $(this).siblings('a');
      $(this).appendTo(menuLink);
    })

    $('.sublist-toggle').on('click', function (e) {
      e.stopPropagation();
      var parentItem = $(this).parents('.mm-nav-link');
      parentItem.siblings('.sublist').addClass('active');
    })
  }
})



