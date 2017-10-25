//facet javascript code here.

function GetFacetDataScript(me) {

    //var categoriesValue = $("#checkBoxCategoriesId").val();
    //var subCategoriesValue = $("#checkBoxSubCategoriesId").val();
    //var descendingValue = $("#DescendingInput").prop("checked");

    //var categoriesValue = $("input[name='SelectedCategories']")
    //        .map(function () { return $(this).val(); }).get();

    //var subCategoriesValue = $("input[name='SelectedSubCategories']")
    //    .map(function () { return $(this).val(); }).get();



    
    var categoryVal, subCategoryVal;
    var queryStringValue = "?";
    var href = window.location.href;
    var pass = true;
    var SelectedQueries = [];


    // Clean the href 
    function cleanHref() {
        if (href.slice(-1) === '?') {
            href = href.replace('?', '');
            window.history.replaceState(null, null, href);

            // update the final url
            href = window.location.href;
        }
    }
    cleanHref();


    // if the query is exist
    if (href.indexOf(',' + $(me).val()) > -1) {
        href = href.replace(',' + $(me).val() , '');
        window.history.replaceState(null, null, href);
        // update the final url
        href = window.location.href;
        pass = false;

    } else if (href.indexOf('selectedCategories=' + $(me).val()) > -1) {

        href = href.replace('selectedCategories=' + $(me).val() , '');
        window.history.replaceState(null, null, href);

        // update the final url
        href = window.location.href;
        // Clean the href 
        cleanHref();
        pass = false;
    } else if (href.indexOf('selectedSubCategories=' + $(me).val()) > -1) {

        href = href.replace('selectedSubCategories=' + $(me).val() , '');
        window.history.replaceState(null, null, href);

        // update the final url
        href = window.location.href;
        // Clean the href 
        cleanHref();
        pass = false;
    }







    // Check if Category have been selected
    if ($(me).is('#checkBoxCategoriesId') && pass ) {
        categoryVal = $(me).val();

        // If its first parameter
        if ( href.indexOf('?') == -1 ) {
            window.history.replaceState(null, null, href + '?selectedCategories=' + categoryVal);

            // update the final url
            href = window.location.href;
        
        } else {
            
            // Check the exist parameters
            if (href.indexOf('?selectedCategories=') == -1 && href.indexOf('&selectedCategories=') == -1 ) {
                window.history.replaceState(null, null, href + '&selectedCategories=' + categoryVal);
                
                // update the final url
                href = window.location.href;
            }

            if (href.indexOf('?selectedCategories=') > -1) {
                var s = href.indexOf('&');
                if (s != -1) { // if the subcategory have been selected as well
                    href = href.substr(0, s) + ',' + categoryVal + href.substr(s);
                    window.history.replaceState(null, null, href);

                    // update the final url
                    href = window.location.href;
                } else {
                    window.history.replaceState(null, null, href + ',' + categoryVal);

                    // update the final url
                    href = window.location.href;    
                }
            }

            if (href.indexOf('&selectedCategories=') > -1) {
                window.history.replaceState(null, null, href + ',' + categoryVal);

                // update the final url
                href = window.location.href;
            }
        }


    } 

    // if its Subcategory checkboxes  
    if ($(me).is('#checkBoxSubCategoriesId') && pass) { 
        subCategoryVal = $(me).val();

        if ( href.indexOf('?') == -1 ) { // if first query
            window.history.replaceState(null, null, href + '?selectedSubCategories=' + subCategoryVal);

            // update the final url
            href = window.location.href;

        } else {
            window.history.replaceState(null, null, href + ',' + subCategoryVal);
            
            // update the final url
            href = window.location.href;
        }
    }


    

    $.ajax({
        type: 'GET',
        url: '/Search/FacetSearchQuery',
        data: {
            selectedCategories: categoryVal,
            selectedSubCategories: subCategoryVal,
            queryString: href
        }
    })
    .done(function (result) {
        $("#outputResult").html(result);
        //$("#outputResultOld").remove();
    })

    .fail(function (xhr, status, error) {
        $("#outputResult").text("Something went wrong...");
    });
}