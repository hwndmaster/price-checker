import { ProductScanStatus } from "@/models/enums";
import { createProductOverview } from "@/utils/tests/testModels";
import * as scansActions from "@/store/scans/actions";
import * as actionsInternal from "./actionsInternal";
import productsReducer from "./reducers";
import ProductsState from "./state";

function createState(overrides?: Partial<ProductsState>): ProductsState {
    return {
        overviews: [],
        prices: {},
        ...overrides,
    };
}

it("setOverviews: replaces the overviews", () => {
    // Arrange
    const overview = createProductOverview();

    // Act
    const state = productsReducer(createState(), actionsInternal.setOverviews([overview]));

    // Assert
    expect(state.overviews).toEqual([overview]);
});

it("productScanStarted: sets the product status to Scanning", () => {
    // Arrange
    const overview = createProductOverview({ status: ProductScanStatus.ScannedOk });

    // Act
    const state = productsReducer(createState({ overviews: [overview] }),
        scansActions.productScanStarted(overview.id));

    // Assert
    expect(state.overviews[0].status).toBe(ProductScanStatus.Scanning);
});

it("productScanFailed: sets the product status to Failed with the status text", () => {
    // Arrange
    const overview = createProductOverview({ status: ProductScanStatus.Scanning });

    // Act
    const state = productsReducer(createState({ overviews: [overview] }),
        scansActions.productScanFailed({ productId: overview.id, errorMessage: "Something went wrong" }));

    // Assert
    expect(state.overviews[0].status).toBe(ProductScanStatus.Failed);
    expect(state.overviews[0].statusText).toBe("Something went wrong");
});

it("productScanFinished: replaces the overview row", () => {
    // Arrange
    const overview = createProductOverview({ status: ProductScanStatus.Scanning });
    const finishedOverview = { ...overview, status: ProductScanStatus.ScannedNewLowest, lowestPrice: 99 };

    // Act
    const state = productsReducer(createState({ overviews: [overview] }),
        scansActions.productScanFinished(finishedOverview));

    // Assert
    expect(state.overviews).toEqual([finishedOverview]);
});
